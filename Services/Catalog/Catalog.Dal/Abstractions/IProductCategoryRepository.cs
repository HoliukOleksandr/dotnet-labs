using Catalog.Domain.Entities;

namespace Catalog.Dal.Abstractions;

public interface IProductCategoryRepository : IGenericRepository<ProductCategory, (long ProductId, int CategoryId)>
{
    Task<bool> HasPrimaryAsync(long productId, CancellationToken ct = default);

    /// <summary>LINQ to Entities: M:N через проміжну сутність і її власне поле AddedAt.</summary>
    Task<IReadOnlyList<Product>> GetRecentlyAddedAsync(string categoryName, DateTime since, CancellationToken ct = default);
}
