namespace Orders.Domain.Exceptions;

/// <summary>Порушено бізнес-правило (недопустимий перехід статусу, нестача товару, дублікат) -> HTTP 409.</summary>
public class BusinessConflictException : Exception
{
    public BusinessConflictException(string message) : base(message) { }
    public BusinessConflictException(string message, Exception inner) : base(message, inner) { }
}
