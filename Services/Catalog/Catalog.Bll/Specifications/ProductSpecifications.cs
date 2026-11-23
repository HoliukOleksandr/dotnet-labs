using Ardalis.Specification;
using Catalog.Bll.Dtos;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

/// <summary>Спільні критерії фільтрації списку товарів (без пагінації й сортування).</summary>
public abstract class ProductsCriteriaSpec : Specification<Product>
{
    protected void ApplyCriteria(ProductQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim();
            Query.Where(p => p.Name.Contains(search));
        }
        if (q.MinPrice is not null)
        {
            var min = q.MinPrice.Value;
            Query.Where(p => p.Price >= min);
        }
        if (q.MaxPrice is not null)
        {
            var max = q.MaxPrice.Value;
            Query.Where(p => p.Price <= max);
        }
    }
}

/// <summary>Сторінка товарів: фільтр + сортування (лише дозволені поля) + Skip/Take. Виконується одним SQL-запитом.</summary>
public sealed class ProductsFilterSpec : ProductsCriteriaSpec
{
    public ProductsFilterSpec(ProductQuery q, int page, int pageSize)
    {
        ApplyCriteria(q);

        var desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        switch (q.SortBy.ToLowerInvariant())
        {
            case "price":
                if (desc) Query.OrderByDescending(p => p.Price); else Query.OrderBy(p => p.Price);
                break;
            case "stock":
                if (desc) Query.OrderByDescending(p => p.AvailableStock); else Query.OrderBy(p => p.AvailableStock);
                break;
            default:
                if (desc) Query.OrderByDescending(p => p.Name); else Query.OrderBy(p => p.Name);
                break;
        }

        Query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}

/// <summary>Ті самі критерії без пагінації — для підрахунку totalCount у БД.</summary>
public sealed class ProductsCountSpec : ProductsCriteriaSpec
{
    public ProductsCountSpec(ProductQuery q) => ApplyCriteria(q);
}

/// <summary>Друга специфікація з іншими критеріями: товари, що закінчуються. Працює через той самий WithSpecification().</summary>
public sealed class LowStockProductsSpec : Specification<Product>
{
    public LowStockProductsSpec(int threshold)
    {
        Query.Where(p => p.IsActive && p.AvailableStock <= threshold)
             .OrderBy(p => p.AvailableStock)
             .ThenBy(p => p.Name);
    }
}
