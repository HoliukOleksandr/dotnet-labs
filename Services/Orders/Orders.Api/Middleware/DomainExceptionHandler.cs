using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Orders.Domain.Exceptions;

namespace Orders.Api.Middleware;

/// <summary>
/// Єдине місце перетворення винятків на HTTP-статуси у форматі ProblemDetails (RFC 9457).
/// Ланцюжок: THROW 50002 у процедурі -> SqlException -> BusinessConflictException (DAL) -> 409 тут.
/// </summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title, isDomain) = ex switch
        {
            NotFoundException         => (StatusCodes.Status404NotFound, "Resource not found", true),
            BusinessConflictException => (StatusCodes.Status409Conflict, "Business rule conflict", true),
            ValidationException       => (StatusCodes.Status400BadRequest, "Validation failed", true),
            _                         => (StatusCodes.Status500InternalServerError, "Unexpected server error", false)
        };

        if (!isDomain)
            logger.LogError(ex, "Unhandled exception");   // деталі лише в лог, не у відповідь

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = isDomain ? ex.Message : null
            }
        });
    }
}
