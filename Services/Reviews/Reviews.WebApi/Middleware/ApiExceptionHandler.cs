using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Domain.Exceptions;

namespace Reviews.WebApi.Middleware;

/// <summary>
/// Глобальна обробка помилок у форматі ProblemDetails (RFC 9457), включно з MongoDB-винятками.
/// NotFound -> 404; ValidationException -> 400; DomainException -> 400; Conflict (у т. ч. версії) -> 409;
/// MongoWriteException DuplicateKey -> 409 (інші помилки запису -> 400); MongoConnectionException -> 503; решта -> 500.
/// Виняток логується один раз — тут.
/// </summary>
public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment env,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violated"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
                => (StatusCodes.Status409Conflict, "Record with the same key already exists"),
            MongoWriteException => (StatusCodes.Status400BadRequest, "Write operation failed"),
            MongoConnectionException or TimeoutException
                => (StatusCodes.Status503ServiceUnavailable, "Database is unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error")
        };

        if (status >= 500)
            logger.LogError(ex, "Request {Path} failed", http.Request.Path);
        else
            logger.LogWarning("Request {Path} rejected: {Status} {ErrorType}", http.Request.Path, status, ex.GetType().Name);

        http.Response.StatusCode = status;

        var isClientError = ex is NotFoundException or ValidationException or DomainException or ConflictException;
        var detail = isClientError ? null : (env.IsDevelopment() ? ex.Message : null);

        ProblemDetails body;
        if (ex is ValidationException validation)
        {
            // усі помилки валідації — разом, по полях
            var errors = validation.Errors
                .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "request" : e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
            body = new ValidationProblemDetails(errors) { Status = status, Title = title };
        }
        else
        {
            body = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = ex is NotFoundException or DomainException or ConflictException ? ex.Message : detail
            };
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = body
        });
    }
}
