using System.Linq.Expressions;
using Fms.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Fms.Repository;

/// <summary>EF Core context over the FMS PostgreSQL schema (feature 02). Lives in the
/// Repository layer per docs/backend-standard.md — no other layer touches EF Core.</summary>
public class FmsDbContext(DbContextOptions<FmsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Maps a CLR <c>string</c> id to a PostgreSQL <c>uuid</c> column. The .NET
    /// model holds ids as uuid strings (backend-standard §6.1.1); the converter bridges
    /// string ⇄ Guid so Npgsql stores/reads a real <c>uuid</c> type.</summary>
    private static readonly ValueConverter<string, Guid> UuidConverter =
        new(v => Guid.Parse(v), v => v.ToString());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // users — Entra object id is the unique external identity (feature 03 provisions)
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            MapUuidId(e, u => u.Id, databaseGenerated: true);
            e.Property(u => u.EntraObjectId).HasColumnName("entraObjectId")
                .IsRequired().HasMaxLength(200);
            e.HasIndex(u => u.EntraObjectId).IsUnique();
            e.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
            e.Property(u => u.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            e.Property(u => u.Role).HasColumnName("role")
                .IsRequired().HasMaxLength(20).HasDefaultValue("user");
            MapAudit(e);
        });

        // spaces
        modelBuilder.Entity<Space>(e =>
        {
            e.ToTable("spaces");
            MapUuidId(e, s => s.Id, databaseGenerated: true);
            e.Property(s => s.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            MapAudit(e);
        });

        // forms — belong to a space; definition is standard JSON Schema stored as jsonb
        modelBuilder.Entity<Form>(e =>
        {
            e.ToTable("forms");
            MapUuidId(e, f => f.Id, databaseGenerated: true);
            MapUuidId(e, f => f.SpaceId);
            e.HasOne(f => f.Space).WithMany(s => s.Forms).HasForeignKey(f => f.SpaceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(f => f.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            e.Property(f => f.Schema).HasColumnName("schema").HasColumnType("jsonb").IsRequired();
            MapAudit(e);
        });

        // submissions — belong to a form + user; data stored as jsonb
        modelBuilder.Entity<Submission>(e =>
        {
            e.ToTable("submissions");
            MapUuidId(e, s => s.Id, databaseGenerated: true);
            MapUuidId(e, s => s.FormId);
            MapUuidId(e, s => s.UserId);
            e.HasOne(s => s.Form).WithMany(f => f.Submissions).HasForeignKey(s => s.FormId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.User).WithMany(u => u.Submissions).HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(s => s.Data).HasColumnName("data").HasColumnType("jsonb").IsRequired();
            MapAudit(e);
        });

        // permissions — resourceType/resourceId target, expression evaluated at request time
        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("permissions");
            MapUuidId(e, p => p.Id, databaseGenerated: true);
            e.Property(p => p.ResourceType).HasColumnName("resourceType").IsRequired().HasMaxLength(20);
            e.Property(p => p.ResourceId).HasColumnName("resourceId").IsRequired().HasMaxLength(64); // uuid text or "*"
            e.Property(p => p.Expression).HasColumnName("expression").IsRequired().HasColumnType("text");
            MapAudit(e);
        });
    }

    /// <summary>Maps a primary key or foreign-key id property to a <c>uuid</c> column
    /// named after the property in camelCase (<c>SpaceId</c> → <c>spaceId</c>). When
    /// <paramref name="databaseGenerated"/> is true (a primary key) the column also gets
    /// a client-side <see cref="ClientUuidGenerator"/> (the InMemory provider used by unit
    /// tests cannot run a SQL default) plus a <c>gen_random_uuid()</c> DB default so
    /// direct SQL inserts stay consistent.</summary>
    private static void MapUuidId<TEntity>(
        EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, string>> property, bool databaseGenerated = false)
        where TEntity : class
    {
        var builder = entity.Property(property)
            .HasColumnName(Camel(PropertyName(property)))
            .HasColumnType("uuid")
            .HasConversion(UuidConverter);
        if (databaseGenerated)
        {
            builder.HasValueGenerator<ClientUuidGenerator>()
                .HasDefaultValueSql("gen_random_uuid()");
        }
    }

    /// <summary>Produces a uuid string (Guid.NewGuid) for a generated key before the row
    /// is written. EF sends the value on insert; the <c>gen_random_uuid()</c> DB default
    /// above remains as a safety net for raw SQL inserts.</summary>
    private sealed class ClientUuidGenerator : ValueGenerator<string>
    {
        public override bool GeneratesTemporaryValues => false;

        public override string Next(EntityEntry entry) => Guid.NewGuid().ToString();
    }

    /// <summary>Maps the shared audit columns (backend-standard §6.1.1): created/last-
    /// modified actor (Entra object id) and UTC timestamps with a DB default so direct
    /// SQL inserts stay consistent even outside EF. Column names are camelCase.</summary>
    private static void MapAudit<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class, IAuditable
    {
        entity.Property(a => a.CreatedBy).HasColumnName("createdBy").HasColumnType("text");
        entity.Property(a => a.CreatedOn).HasColumnName("createdOn")
            .HasColumnType("timestamptz").HasDefaultValueSql("now()");
        entity.Property(a => a.LastModifiedBy).HasColumnName("lastModifiedBy").HasColumnType("text");
        entity.Property(a => a.LastModifiedOn).HasColumnName("lastModifiedOn")
            .HasColumnType("timestamptz").HasDefaultValueSql("now()");
    }

    private static string PropertyName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property) =>
        property.Body is MemberExpression member
            ? member.Member.Name
            : throw new ArgumentException($"Unsupported property expression: {property}");

    /// <summary>camelCase a PascalCase property name: <c>EntraObjectId</c> → <c>entraObjectId</c>.</summary>
    private static string Camel(string name) =>
        name.Length <= 1 ? name.ToLowerInvariant() : char.ToLowerInvariant(name[0]) + name[1..];
}
