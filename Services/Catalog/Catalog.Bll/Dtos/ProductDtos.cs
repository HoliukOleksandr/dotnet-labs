using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public record ProductDto(long Id, string Name, string Sku, decimal Price, int AvailableStock, bool IsActive);

public record ProductDetailDto(string Description, string? Manufacturer, int? WeightGrams, int WarrantyMonths);

public record ProductImageDto(long Id, string Url, int SortOrder, bool IsMain);

public class ProductCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public DateTime AddedAt { get; set; }
    public bool IsPrimary { get; set; }
}

/// <summary>Картка товару (Eager Loading): деталі, картинки й категорії одним запитом.</summary>
public class ProductDetailsDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public bool IsActive { get; set; }
    public ProductDetailDto? Detail { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductCategoryDto> Categories { get; set; } = new();
}

/// <summary>
/// Вхідний контракт. Прості правила (обов'язковість і довжина назви) — DataAnnotations;
/// складніші (формат SKU, ціна) — FluentValidation (CreateProductDtoValidator). Правила не дублюються.
/// </summary>
public class CreateProductDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = "";

    public string Sku { get; set; } = "";

    public decimal Price { get; set; }
}

public class ChangePriceDto
{
    public decimal Price { get; set; }
}

public class StockChangeDto
{
    public int Quantity { get; set; }
}

public class AssignCategoryDto
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
    public bool IsPrimary { get; set; }
}

/// <summary>Параметри списку: пагінація, фільтрація, сортування.</summary>
public record ProductQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string SortBy = "name",
    string SortDir = "asc");

/// <summary>Метадані пагінації — у тілі відповіді (а також у заголовках X-Total-Count, X-Page, X-Page-Size).</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
