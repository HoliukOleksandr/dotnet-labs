using Catalog.Dal.Abstractions;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class CategoryRepository(CatalogDbContext context)
    : GenericRepository<Category, int>(context), ICategoryRepository
{
    // Explicit Loading: товари категорії довантажуються окремо, лише коли справді потрібні
    public async Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default)
    {
        var category = await Set.FirstOrDefaultAsync(c => c.Id == id, ct);      // 1-й запит
        if (category is null) return null;

        await Context.Entry(category).Collection(c => c.ProductCategories)
            .Query().Include(pc => pc.Product)
            .LoadAsync(ct);                                                      // 2-й запит
        return category;
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => Set.AnyAsync(c => c.Name == name, ct);
}
