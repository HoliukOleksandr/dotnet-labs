using Catalog.Dal.Abstractions;
using Catalog.Dal.Repositories;

namespace Catalog.Dal;

/// <summary>
/// Unit of Work над EF Core: репозиторії лише додають/змінюють/видаляють сутності в контексті,
/// а SaveChangesAsync() викликається один раз наприкінці бізнес-операції (одна транзакція).
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context;
        Products = new ProductRepository(context);
        Categories = new CategoryRepository(context);
        ProductCategories = new ProductCategoryRepository(context);
    }

    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public IProductCategoryRepository ProductCategories { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
