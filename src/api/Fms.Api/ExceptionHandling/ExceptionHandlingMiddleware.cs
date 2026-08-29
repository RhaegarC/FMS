using Fms.Api.Contracts;
using Fms.Service.Exceptions;

namespace Fms.Api.ExceptionHandling;

/// <summary>Maps domain exceptions raised by the Service layer to HTTP responses.
/// Runs first in the pipeline so every endpoint benefits: a
/// <see cref="ValidationException"/> becomes 400 with an <c>ApiError</c> body, a
/// <see cref="NotFoundException"/> becomes 404 (empty, like <c>Results.NotFound()</c>),
/// and a <see cref="ForbiddenException"/> becomes 403 (empty, like
/// <c>Results.Forbid()</c>). Unknown exceptions propagate to ASP.NET Core's 500.</summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, new ApiError(ex.Message));
        }
        catch (NotFoundException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
        catch (ForbiddenException)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
            throw;
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, ApiError body)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(body);
    }
}
