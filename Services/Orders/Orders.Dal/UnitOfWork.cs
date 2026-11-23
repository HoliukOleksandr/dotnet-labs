using System.Data;
using Microsoft.Data.SqlClient;
using Orders.Dal.Abstractions;
using Orders.Dal.Repositories;

namespace Orders.Dal;

/// <summary>
/// Unit of Work: одне з'єднання на всю операцію, явна транзакція з обраним рівнем ізоляції.
/// Реєструється як Scoped (один екземпляр і одне з'єднання на HTTP-запит).
///
/// Рівень ізоляції за замовчуванням — ReadCommitted: немає «брудних» читань, а блокувань мінімум.
/// Вищі рівні (RepeatableRead, Serializable) зменшують аномалії читання, але збільшують блокування
/// і ризик взаємних блокувань (deadlock). Таймаути задаються в рядку підключення:
/// "Connect Timeout=15; Command Timeout=30".
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
        // усі репозиторії отримують ОДНЕ з'єднання і доступ до ПОТОЧНОЇ транзакції
        Customers = new CustomerRepository(_connection, () => _transaction);
        Orders = new OrderRepository(_connection, () => _transaction);
        Products = new ProductRepository(_connection, () => _transaction);
    }

    public ICustomerRepository Customers { get; }
    public IOrderRepository Orders { get; }
    public IProductRepository Products { get; }

    public async Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Transaction is already started");
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(ct);
        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(level, ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction");
        var tx = _transaction;
        try
        {
            await tx.CommitAsync(ct);
        }
        finally
        {
            _transaction = null;
            await tx.DisposeAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        var tx = _transaction;
        if (tx is null) return;
        _transaction = null;
        try
        {
            // якщо сервер уже відкотив транзакцію (XACT_ABORT у процедурі), Connection == null
            if (tx.Connection is not null)
                await tx.RollbackAsync(ct);
        }
        catch (Exception ex) when (ex is InvalidOperationException or SqlException)
        {
            // транзакція вже завершена на боці сервера — нічого відкочувати
        }
        finally
        {
            await tx.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await RollbackAsync();
        }
        await _connection.DisposeAsync();
    }
}
