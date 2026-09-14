namespace Fms.Model.DatabaseEntity;

/// <summary>A form definition (a.k.a. dataset), belonging to a space.</summary>
public sealed class Form : EntityBase
{
    public string SpaceId { get; set; } = string.Empty;

    public Space Space { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>Standard JSON Schema (draft 2020-12), stored as <c>jsonb</c>.</summary>
    public string Schema { get; set; } = string.Empty;

    public List<Submission> Submissions { get; } = [];
}
