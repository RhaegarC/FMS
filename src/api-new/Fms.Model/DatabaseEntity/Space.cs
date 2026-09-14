namespace Fms.Model.DatabaseEntity;

/// <summary>Top-level grouping of forms; the coarsest permission target.</summary>
public sealed class Space : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public List<Form> Forms { get; } = [];
}
