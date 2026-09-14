namespace Fms.Repository;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Model.DatabaseEntity;
using System.Linq.Expressions;

/// <summary>EF Core context over the FMS PostgreSQL schema. Lives in the Repository layer
/// per STANDARD.md — no other layer touches EF Core.</summary>
public class FmsDbContext(DbContextOptions<FmsDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Maps a CLR <c>string</c> id to a PostgreSQL <c>uuid</c> column. The .NET
    /// model holds ids as uuid strings; the converter bridges string ⇄ Guid so Npgsql
    /// stores and reads a real <c>uuid</c> type.</summary>
    private static readonly ValueConverter<string, Guid> UuidConverter =
        new(v => Guid.Parse(v), v => v.ToString());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            // An audit table is usually the fastest-growing table in a system, and it gets
            // read two ways: "what happened to this row" and "what happened around then".
            // Without an index each of those is a sequential scan of the whole history.
            entity.HasIndex(log => new { log.TableName, log.EntityId });
            entity.HasIndex(log => log.Timestamp);

            // Npgsql maps string to text by default, which makes the snapshots
            // write-only. jsonb validates the JSON on the way in and stays queryable
            // afterwards -- the difference between a searchable history and a pile of
            // opaque blobs. Both columns hold JSON or null.
            entity.Property(log => log.OldValues).HasColumnType("jsonb");
            entity.Property(log => log.NewValues).HasColumnType("jsonb");
        });

        // users — Id *is* the Entra object id, so there is no external-identity column and
        // no unique index to keep in step with it.
        modelBuilder.Entity<User>(entity =>
        {
            MapEntity(entity, "users");
            entity.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
            entity.Property(u => u.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(u => u.Role).HasColumnName("role").IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<Space>(entity =>
        {
            MapEntity(entity, "spaces");
            entity.Property(s => s.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        });

        // forms — belong to a space; the definition is standard JSON Schema stored as jsonb
        modelBuilder.Entity<Form>(entity =>
        {
            MapEntity(entity, "forms");
            MapUuidId(entity, f => f.SpaceId);
            entity.HasOne(f => f.Space).WithMany(s => s.Forms).HasForeignKey(f => f.SpaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(f => f.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(f => f.Schema).HasColumnName("schema").HasColumnType("jsonb").IsRequired();
        });

        // submissions — belong to a form and a user; the data is stored as jsonb
        modelBuilder.Entity<Submission>(entity =>
        {
            MapEntity(entity, "submissions");
            MapUuidId(entity, s => s.FormId);
            MapUuidId(entity, s => s.UserId);
            entity.HasOne(s => s.Form).WithMany(f => f.Submissions).HasForeignKey(s => s.FormId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.User).WithMany(u => u.Submissions).HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(s => s.Data).HasColumnName("data").HasColumnType("jsonb").IsRequired();
        });

        // permissions — resourceType/resourceId target, expression evaluated at request time
        modelBuilder.Entity<Permission>(entity =>
        {
            MapEntity(entity, "permissions");
            entity.Property(p => p.ResourceType).HasColumnName("resourceType").IsRequired().HasMaxLength(20);
            // A uuid as text, or "*" for every resource of the type — so not a uuid column.
            entity.Property(p => p.ResourceId).HasColumnName("resourceId").IsRequired().HasMaxLength(64);
            entity.Property(p => p.Expression).HasColumnName("expression").IsRequired().HasColumnType("text");
        });

        ApplySoftDeleteFilter(modelBuilder);
    }

    /// <summary>Maps the primary key and shared audit columns every
    /// <see cref="EntityBase"/> carries. The key is <c>uuid</c> but application-assigned:
    /// no <c>gen_random_uuid()</c> default and no value generator, so the id exists before
    /// the save and the interceptor can record it in the audit trail for an insert.
    /// Timestamps likewise have no <c>now()</c> default — the interceptor is the single
    /// source of truth for both, and a DB default would silently paper over a row that
    /// never went through it.</summary>
    private static void MapEntity<TEntity>(EntityTypeBuilder<TEntity> entity, string table)
        where TEntity : EntityBase
    {
        entity.ToTable(table);
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasConversion(UuidConverter);
        entity.Property(e => e.CreatedBy).HasColumnName("createdBy").HasColumnType("text");
        entity.Property(e => e.CreatedOn).HasColumnName("createdOn").HasColumnType("timestamptz");
        entity.Property(e => e.LastModifiedBy).HasColumnName("lastModifiedBy").HasColumnType("text");
        entity.Property(e => e.LastModifiedOn).HasColumnName("lastModifiedOn").HasColumnType("timestamptz");
        entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
    }

    /// <summary>Maps a foreign-key id to a <c>uuid</c> column named after the property in
    /// camelCase (<c>SpaceId</c> → <c>spaceId</c>).</summary>
    private static void MapUuidId<TEntity>(
        EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, string>> property)
        where TEntity : class
    {
        entity.Property(property)
            .HasColumnName(Camel(PropertyName(property)))
            .HasColumnType("uuid")
            .HasConversion(UuidConverter);
    }

    private static string PropertyName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property) =>
        property.Body is MemberExpression member
            ? member.Member.Name
            : throw new ArgumentException($"Unsupported property expression: {property}");

    /// <summary>camelCase a PascalCase property name: <c>SpaceId</c> → <c>spaceId</c>.</summary>
    private static string Camel(string name) =>
        name.Length <= 1 ? name.ToLowerInvariant() : char.ToLowerInvariant(name[0]) + name[1..];

    /// <summary>
    /// Reads every <see cref="EntityBase"/> type through "not soft-deleted", so a row that
    /// <c>DeleteAsync</c> marked is genuinely gone from ordinary queries rather than merely
    /// flagged. Applied by convention across the model rather than named entity by entity,
    /// so a type added later inherits the filter instead of quietly returning deleted rows.
    /// </summary>
    /// <remarks>
    /// To see deleted rows anyway, the escape hatch is EF's own
    /// <c>IgnoreQueryFilters()</c> on the query — <c>Users.IgnoreQueryFilters()</c>. It is
    /// deliberately explicit: reading history back is a decision, not a default.
    /// </remarks>
    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(EntityBase).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            // entity => entity.IsDeleted != true
            ParameterExpression entity = Expression.Parameter(entityType.ClrType, "entity");
            Expression notDeleted = Expression.NotEqual(
                Expression.Property(entity, nameof(EntityBase.IsDeleted)),
                Expression.Constant(true, typeof(bool?)));

            entityType.SetQueryFilter(Expression.Lambda(notDeleted, entity));
        }
    }
}
