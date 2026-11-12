using Orders.Domain.Models;

namespace Orders.Dal.Abstractions;

/// <summary>
/// Репозиторій на ЧИСТОМУ ADO.NET. Свідомо НЕ успадковує IGenericRepository&lt;T&gt;
/// (демонструє ручну роботу з підключенням, параметрами й транзакцією).
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<long> AddAsync(Customer customer, CancellationToken ct = default);
    Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);   // м'яке видалення
}
