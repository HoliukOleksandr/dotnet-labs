namespace Orders.Domain.Exceptions;

/// <summary>Ресурс не знайдено -> HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string message, Exception inner) : base(message, inner) { }
}
