namespace Orders.Domain.Exceptions;

/// <summary>Некоректні вхідні дані -> HTTP 400.</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception inner) : base(message, inner) { }
}
