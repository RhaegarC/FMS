namespace Fms.Service.Exceptions;

/// <summary>Invalid request input — mapped to HTTP 400 with an <c>ApiError</c> body
/// by the Api layer's exception-handling middleware.</summary>
public sealed class ValidationException(string message) : Exception(message);

/// <summary>Requested resource does not exist — mapped to HTTP 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>The caller lacks permission — mapped to HTTP 403.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
