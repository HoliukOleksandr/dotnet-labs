using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Abstractions;

namespace Orders.Dal.Repositories;

/// <summary>
/// Спільний CRUD на Dapper. Ім'я таблиці й фільтр передаються з коду (константи), а не з вхідних даних
/// користувача, тому їх підстановка в текст запиту безпечна. Усі ЗНАЧЕННЯ передаються лише параметрами.
/// </summary>
public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    private readonly string _table;

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> tx, string tableName)
        : base(connection, tx)
    {
        _table = tableName;
    }

    /// <summary>Додатковий фільтр «живих» записів, напр. " AND IsDeleted = 0".</summary>
    protected virtual string ActiveFilter => "";

    protected CommandDefinition Cmd(string sql, object? param = null,
                                    CommandType? type = null, CancellationToken ct = default)
        => new(sql, param, Transaction, commandType: type, cancellationToken: ct);

    public virtual Task<T?> GetByIdAsync(long id, CancellationToken ct = default) => GuardAsync<T?>(async () =>
    {
        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<T>(
            Cmd($"SELECT * FROM {_table} WHERE Id = @Id{ActiveFilter}", new { Id = id }, ct: ct));
    });

    public virtual Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => GuardAsync<IReadOnlyList<T>>(async () =>
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<T>(Cmd($"SELECT * FROM {_table} WHERE 1 = 1{ActiveFilter}", ct: ct));
        return rows.ToList();
    });

    public abstract Task<long> AddAsync(T entity, CancellationToken ct = default);

    public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct = default);

    public virtual Task<bool> DeleteAsync(long id, CancellationToken ct = default) => GuardAsync<bool>(async () =>
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.ExecuteAsync(Cmd($"DELETE FROM {_table} WHERE Id = @Id", new { Id = id }, ct: ct));
        return rows > 0;
    });
}
