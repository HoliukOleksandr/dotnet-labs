namespace Reviews.Domain.Exceptions;

/// <summary>Порушено бізнес-правило сутності -> HTTP 400.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Ресурс не знайдено -> HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Конфлікт стану (зокрема конфлікт версій документа) -> HTTP 409.</summary>
public class ConflictException(string message) : Exception(message);
