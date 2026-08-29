namespace Fms.Interface.Repository;

/// <summary>Filter for a submission list query. <see cref="From"/>/<see cref="To"/> are
/// already-parsed UTC bounds (date-only <c>to</c> is exclusive, next midnight); the
/// keyword search runs against the jsonb text representation.</summary>
public sealed record SubmissionQuery(
    int? FormId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Keyword = null,
    int? UserId = null,
    IReadOnlySet<int>? AccessibleFormIds = null);
