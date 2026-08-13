namespace Fms.Api.Data.Entities;

/// <summary>Top-level grouping of forms; the coarsest permission target.</summary>
public class Space
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public List<Form> Forms { get; } = [];
}
