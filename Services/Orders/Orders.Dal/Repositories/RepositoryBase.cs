using System.Data;
using Microsoft.Data.SqlClient;
using Orders.Dal.Internal;

namespace Orders.Dal.Repositories;

/// <summary>Спільна база: з'єднання й ПОТОЧНА транзакція Unit of Work (а не рядок підключення).</summary>
public abstract class RepositoryBase
{
    protected readonly SqlConnection Connection;
    private readonly Func<SqlTransaction?> _currentTransaction;

    protected RepositoryBase(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    {
        Connection = connection;
        _currentTransaction = currentTransaction;
    }

    /// <summary>Поточна транзакція Unit of Work (або null, якщо її не розпочато).</summary>
    protected SqlTransaction? Transaction => _currentTransaction();

    protected async Task EnsureOpenAsync(CancellationToken ct)
    {
        if (Connection.State != ConnectionState.Open)
            await Connection.OpenAsync(ct);
    }

    protected async Task<TResult> GuardAsync<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            return await action();
        }
        catch (SqlException ex)
        {
            var domain = SqlExceptionTranslator.Translate(ex);
            if (domain is null) throw;
            throw domain;
        }
    }

    protected async Task GuardAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (SqlException ex)
        {
            var domain = SqlExceptionTranslator.Translate(ex);
            if (domain is null) throw;
            throw domain;
        }
    }
}
