using Fms.Model.Entities;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // users — Entra object id is the unique external identity (feature 03 provisions)
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(u => u.EntraObjectId).IsUnique();
            e.Property(u => u.EntraObjectId).IsRequired().HasMaxLength(200);
            e.Property(u => u.Email).IsRequired().HasMaxLength(320);
            e.Property(u => u.Name).IsRequired().HasMaxLength(200);
            e.Property(u => u.Role).IsRequired().HasMaxLength(20).HasDefaultValue("user");
        });

        // spaces
        modelBuilder.Entity<Space>(e =>
        {
            e.ToTable("spaces");
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
        });

        // forms — belong to a space; definition is standard JSON Schema stored as jsonb
        modelBuilder.Entity<Form>(e =>
        {
            e.ToTable("forms");
            e.HasOne(f => f.Space).WithMany(s => s.Forms).HasForeignKey(f => f.SpaceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(f => f.Name).IsRequired().HasMaxLength(200);
            e.Property(f => f.Schema).HasColumnType("jsonb").IsRequired();
        });

        // submissions — belong to a form + user; data stored as jsonb
        modelBuilder.Entity<Submission>(e =>
        {
            e.ToTable("submissions");
            e.HasOne(s => s.Form).WithMany(f => f.Submissions).HasForeignKey(s => s.FormId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.User).WithMany(u => u.Submissions).HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(s => s.Data).HasColumnType("jsonb").IsRequired();
        });

        // permissions — resource_type/resource_id target, expression evaluated at request time
        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("permissions");
            e.Property(p => p.ResourceType).IsRequired().HasMaxLength(20);
            e.Property(p => p.ResourceId).IsRequired().HasMaxLength(64); // id text or "*"
            e.Property(p => p.Expression).IsRequired().HasColumnType("text");
        });

        // Audit columns (§6.1.1): the same four on every table.
        ConfigureAudit<User>(modelBuilder);
        ConfigureAudit<Space>(modelBuilder);
        ConfigureAudit<Form>(modelBuilder);
        ConfigureAudit<Submission>(modelBuilder);
        ConfigureAudit<Permission>(modelBuilder);
    }

    /// <summary>Maps the shared audit columns (backend-standard §6.1.1): created/last-
    /// modified actor (Entra object id) and UTC timestamps with a DB default so direct
    /// SQL inserts stay consistent even outside EF.</summary>
    private static void ConfigureAudit<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IAuditable
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.Property(a => a.CreatedBy).HasColumnType("text");
            entity.Property(a => a.CreatedOn).HasColumnType("timestamptz").HasDefaultValueSql("now()");
            entity.Property(a => a.LastModifiedBy).HasColumnType("text");
            entity.Property(a => a.LastModifiedOn).HasColumnType("timestamptz").HasDefaultValueSql("now()");
        });
    }
}
