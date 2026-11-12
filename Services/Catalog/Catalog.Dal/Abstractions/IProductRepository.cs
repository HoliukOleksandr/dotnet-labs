using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Dal.Abstractions;

public interface IProductRepository : IGenericRepository<Product, long>
{
    /// <summary>Eager Loading: картка товару з усім пов'язаним ОДНИМ запитом.</summary>
    Task<Product?> GetWithDetailsAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);
    /// <summary>Вибірка за специфікацією (фільтр, сортування, пагінація — на рівні SQL).</summary>
    Task<IReadOnlyList<Product>> ListAsync(ISpecification<Product> spec, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<Product> spec, CancellationToken ct = default);
}
