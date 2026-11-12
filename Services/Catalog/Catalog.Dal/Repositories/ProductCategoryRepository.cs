using Catalog.Dal.Abstractions;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class ProductCategoryRepository(CatalogDbContext context)
    : GenericRepository<ProductCategory, (long ProductId, int CategoryId)>(context), IProductCategoryRepository
{
    // складений ключ: FindAsync приймає обидві частини ключа
    public override async Task<ProductCategory?> GetByIdAsync((long ProductId, int CategoryId) id, CancellationToken ct = default)
        => await Set.FindAsync(new object[] { id.ProductId, id.CategoryId }, ct);

    public Task<bool> HasPrimaryAsync(long productId, CancellationToken ct = default)
        => Set.AnyAsync(pc => pc.ProductId == productId && pc.IsPrimary, ct);

    // M:N через проміжну сутність і її власне поле AddedAt
    public async Task<IReadOnlyList<Product>> GetRecentlyAddedAsync(
        string categoryName, DateTime since, CancellationToken ct = default)
        => await Set
            .Where(pc => pc.Category.Name == categoryName && pc.AddedAt >= since)
            .OrderByDescending(pc => pc.AddedAt)
            .Select(pc => pc.Product)
            .AsNoTracking()
            .ToListAsync(ct);
}
