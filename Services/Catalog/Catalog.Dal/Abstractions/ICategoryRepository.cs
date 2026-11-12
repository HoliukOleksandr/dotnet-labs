using Catalog.Domain.Entities;

namespace Catalog.Dal.Abstractions;

public interface ICategoryRepository : IGenericRepository<Category, int>
{
    /// <summary>Explicit Loading: категорія — 1-й запит, її товари довантажуються окремим (2-м) запитом.</summary>
    Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}
