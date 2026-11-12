namespace Catalog.Bll.Dtos;

public record CategoryDto(int Id, string Name, string Slug);

public class CategoryProductDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal ProductPrice { get; set; }
    public DateTime AddedAt { get; set; }
    public bool IsPrimary { get; set; }
}

/// <summary>Категорія з товарами (Explicit Loading).</summary>
public class CategoryWithProductsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public List<CategoryProductDto> Products { get; set; } = new();
}
