namespace Catalog.Domain.Exceptions;

/// <summary>Порушено інваріант сутності (ціна > 0, достатній залишок) -> HTTP 409.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Ресурс не знайдено -> HTTP 404.</summary>
public class EntityNotFoundException(string message) : Exception(message);

/// <summary>Конфлікт стану (дублікат SKU, паралельна зміна) -> HTTP 409.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Некоректні вхідні дані -> HTTP 400 (ValidationProblemDetails).</summary>
public class ValidationException(string message, IDictionary<string, string[]>? errors = null) : Exception(message)
{
    public IDictionary<string, string[]> Errors { get; } = errors ?? new Dictionary<string, string[]>();
}
