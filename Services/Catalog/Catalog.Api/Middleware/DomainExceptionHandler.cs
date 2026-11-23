using Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Middleware;

/// <summary>
/// Глобальна обробка помилок у форматі ProblemDetails (RFC 9457).
/// EntityNotFound -> 404; Validation -> 400; Conflict, Domain, паралельна зміна, дублікат ключа -> 409; решта -> 500.
/// Виняток логується ОДИН раз — тут. Деталі 500-ї помилки показуються лише в Development.
/// </summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment env,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violated"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was modified by another user"),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } }
                => (StatusCodes.Status409Conflict, "Record with the same key already exists"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error")
        };

        if (status >= 500)
            logger.LogError(ex, "Request {Path} failed", http.Request.Path);
        else
            logger.LogWarning("Request {Path} rejected: {Status} {Reason}", http.Request.Path, status, ex.Message);

        http.Response.StatusCode = status;

        var isDomain = ex is EntityNotFoundException or ConflictException or DomainException or ValidationException;
        var detail = isDomain ? ex.Message : (env.IsDevelopment() ? ex.Message : null);

        ProblemDetails body = ex is ValidationException { Errors.Count: > 0 } validation
            ? new ValidationProblemDetails(validation.Errors) { Status = status, Title = title, Detail = detail }
            : new ProblemDetails { Status = status, Title = title, Detail = detail };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = body
        });
    }
}
