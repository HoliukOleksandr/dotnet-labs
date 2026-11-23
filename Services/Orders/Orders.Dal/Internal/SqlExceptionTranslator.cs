using Microsoft.Data.SqlClient;
using Orders.Domain.Exceptions;

namespace Orders.Dal.Internal;

/// <summary>
/// Перетворює помилки SQL Server на доменні винятки (B.5). Сирі повідомлення SQL у API не передаються:
/// для власних кодів процедур (THROW 500xx) використовується наш текст, для системних — загальний.
/// </summary>
internal static class SqlExceptionTranslator
{
    public static Exception? Translate(SqlException ex) => ex.Number switch
    {
        50001 or 50011 or 50012 => new NotFoundException(ex.Message, ex),
        50002 or 50003 or 50004 or 50013 => new BusinessConflictException(ex.Message, ex),
        50010 => new ValidationException(ex.Message, ex),
        2601 or 2627 => new BusinessConflictException("Record with the same key already exists", ex),
        547 => new BusinessConflictException("Operation violates a data integrity constraint", ex),
        _ => null
    };
}
