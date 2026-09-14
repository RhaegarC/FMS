namespace Fms.Repository.Test;

using Fms.Model.DatabaseEntity;
using Fms.Repository;
using Fms.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
/// The shape of the mapped model, read off the EF metadata with no database involved —
/// building the model never opens a connection, so this runs as fast as the rest of the
/// repository tests.
///
/// These exist because the schema decisions here are the kind that fail silently. A
/// database-generated default, or a key that quietly stops being the identity it is
/// documented as, does not throw: it produces a row that is wrong in a way nobody looks
/// at until it matters.
/// </summary>
public class ModelShapeTests
{
    [Theory]
    [InlineData(typeof(User), "users")]
    [InlineData(typeof(Space), "spaces")]
    [InlineData(typeof(Form), "forms")]
    [InlineData(typeof(Submission), "submissions")]
    [InlineData(typeof(Permission), "permissions")]
    public void Every_entity_maps_to_its_table(Type entity, string table)
    {
        using var context = NewContext();

        Assert.Equal(table, context.Model.FindEntityType(entity)!.GetTableName());
    }

    /// <summary>The key is application-assigned, so the column must carry no default: a
    /// <c>gen_random_uuid()</c> that survived would let an insert succeed with a key the
    /// audit trail had never seen, which is exactly what application assignment prevents.</summary>
    [Theory]
    [InlineData(typeof(User))]
    [InlineData(typeof(Space))]
    [InlineData(typeof(Form))]
    [InlineData(typeof(Submission))]
    [InlineData(typeof(Permission))]
    public void Every_key_is_an_application_assigned_uuid(Type entity)
    {
        using var context = NewContext();

        IProperty id = context.Model.FindEntityType(entity)!.FindProperty(nameof(EntityBase.Id))!;

        Assert.Equal("uuid", id.GetColumnType());
        Assert.Equal("id", id.GetColumnName());
        Assert.Null(id.GetDefaultValueSql());
        Assert.Equal(ValueGenerated.Never, id.ValueGenerated);
    }

    /// <summary>Timestamps come from the interceptor, which is the only thing that knows
    /// who is acting. A <c>now()</c> default would fill the column for writes that never
    /// passed through it, hiding the fact that they were not audited.</summary>
    [Fact]
    public void Timestamps_carry_no_database_default()
    {
        using var context = NewContext();

        foreach (IEntityType entity in EntityTypes(context))
        {
            foreach (var name in new[]
                     {
                         nameof(EntityBase.CreatedOn),
                         nameof(EntityBase.LastModifiedOn),
                     })
            {
                Assert.Null(entity.FindProperty(name)!.GetDefaultValueSql());
            }
        }
    }

    /// <summary>
    /// The user's primary key <em>is</em> the Entra object id, so there is deliberately no
    /// second external-identity column. A reintroduced <c>entraObjectId</c> would give the
    /// same fact two homes, and the two would drift.
    /// </summary>
    [Fact]
    public void The_user_has_no_separate_external_identity_column()
    {
        using var context = NewContext();

        IEntityType user = context.Model.FindEntityType(typeof(User))!;

        Assert.DoesNotContain(
            user.GetProperties(),
            property => property.GetColumnName().Contains("entra", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The filter is applied by convention, so the guard that matters is that it reached
    /// <em>every</em> entity — naming the types here would defeat the point, since a type
    /// added later is the one that would be missed.
    /// </summary>
    [Fact]
    public void Every_entity_is_soft_deleted_by_convention()
    {
        using var context = NewContext();

        foreach (IEntityType entity in EntityTypes(context))
        {
            Assert.NotEmpty(entity.GetDeclaredQueryFilters());
            Assert.Equal("isDeleted", entity.FindProperty(nameof(EntityBase.IsDeleted))!.GetColumnName());
        }
    }

    /// <summary>Every mapped <see cref="EntityBase"/> type — i.e. everything the soft-delete
    /// convention is expected to reach.</summary>
    private static IEnumerable<IEntityType> EntityTypes(FmsDbContext context) =>
        context.Model.GetEntityTypes()
            .Where(entity => typeof(EntityBase).IsAssignableFrom(entity.ClrType));

    private static FmsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<FmsDbContext>()
            .UseNpgsql(AuditHarness.UnreachableConnectionString)
            .Options);
}
