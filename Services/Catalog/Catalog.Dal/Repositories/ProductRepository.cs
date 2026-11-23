using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Catalog.Dal.Abstractions;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class ProductRepository(CatalogDbContext context)
    : GenericRepository<Product, long>(context), IProductRepository
{
    // Eager Loading: Include/ThenInclude — один запит із JOIN
    public Task<Product?> GetWithDetailsAsync(long id, CancellationToken ct = default)
        => Set.AsNoTracking()
              .Include(p => p.Detail)
              .Include(p => p.Images)
              .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
              .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default)
        => Set.AnyAsync(p => p.Sku == sku, ct);

    // Репозиторій лише застосовує специфікацію й виконує ОДИН SQL-запит (OFFSET/FETCH у БД)
    public async Task<IReadOnlyList<Product>> ListAsync(ISpecification<Product> spec, CancellationToken ct = default)
        => await Set.WithSpecification(spec).AsNoTracking().ToListAsync(ct);

    public Task<int> CountAsync(ISpecification<Product> spec, CancellationToken ct = default)
        => Set.WithSpecification(spec).CountAsync(ct);
}
