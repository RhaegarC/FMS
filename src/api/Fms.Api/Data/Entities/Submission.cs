namespace Fms.Api.Data.Entities;

/// <summary>One user's filled-in data for a form, stored as <c>jsonb</c>.</summary>
public class Submission
{
    public int Id { get; set; }

    public int FormId { get; set; }

    public Form Form { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>The submitted values as JSON, stored as <c>jsonb</c>.</summary>
    public string Data { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
