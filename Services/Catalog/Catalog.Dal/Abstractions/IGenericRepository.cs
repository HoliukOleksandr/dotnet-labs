namespace Catalog.Dal.Abstractions;

/// <summary>Async Generic Repository: той самий контракт, що в Лаб. 02, але на EF Core (DbSet&lt;T&gt;).</summary>
public interface IGenericRepository<T, in TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);       // лише змінює стан сутності в контексті -> синхронний
    void Delete(T entity);       // лише змінює стан сутності в контексті -> синхронний
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
