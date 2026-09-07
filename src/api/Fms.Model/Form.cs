namespace Fms.Model.Entities;

/// <summary>A form definition (a.k.a. dataset), belonging to a space.</summary>
public class Form : IAuditable
{
    public int Id { get; set; }

    public int SpaceId { get; set; }

    public Space Space { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Standard JSON Schema (draft 2020-12), stored as <c>jsonb</c>.</summary>
    public string Schema { get; set; } = null!;

    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset LastModifiedOn { get; set; }

    public List<Submission> Submissions { get; } = [];
}
