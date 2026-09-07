using System.Text.Json;

namespace Fms.Api.Contracts;

/// <summary>Payload returned by the <c>/health</c> endpoint.</summary>
public record HealthStatus(string Service, string Status);

/// <summary>Payload returned by <c>/api/me</c> (the authenticated user's profile).</summary>
public record MeResponse(int Id, string Name, string Email, string Role);

// --- Space / form catalog payloads (feature 06) --------------------------

/// <summary>Space as exposed by the catalog API.</summary>
public record SpaceDto(int Id, string Name);

/// <summary>Form definition as exposed by the catalog API.</summary>
public record FormDto(int Id, int SpaceId, string Name, string Schema, DateTimeOffset UpdatedAt);

public record CreateSpaceRequest(string Name);
public record UpdateSpaceRequest(string Name);
public record CreateFormRequest(string Name, string Schema);
public record UpdateFormRequest(string Name, string Schema);

// --- Submission payloads (feature 07) ----------------------------------

/// <summary>Submit body: the form's field values as a JSON object.</summary>
public record SubmitSubmissionRequest(JsonElement Data);

/// <summary>A submission as exposed by the list/export APIs.</summary>
public record SubmissionDto(int Id, int FormId, int UserId, string UserEmail, string Data, DateTimeOffset CreatedAt);

/// <summary>Uniform error payload (validation failures, etc.).</summary>
public record ApiError(string Message);
