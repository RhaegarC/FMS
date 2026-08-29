namespace Fms.Model.Entities;

/// <summary>Top-level grouping of forms; the coarsest permission target.</summary>
public class Space : IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset LastModifiedOn { get; set; }

    public List<Form> Forms { get; } = [];
}
