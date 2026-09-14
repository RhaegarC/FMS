namespace Fms.Model.DatabaseEntity;

/// <summary>One user's filled-in data for a form, stored as <c>jsonb</c>.</summary>
public sealed class Submission : EntityBase
{
    public string FormId { get; set; } = string.Empty;

    public Form Form { get; set; } = null!;

    /// <summary>The submitting user's id — which, for a user, is their Entra object id.</summary>
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    /// <summary>The submitted values as JSON, stored as <c>jsonb</c>.</summary>
    public string Data { get; set; } = string.Empty;
}
