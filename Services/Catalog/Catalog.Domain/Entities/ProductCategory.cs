namespace Catalog.Domain.Entities;

/// <summary>Проміжна сутність M:N із власними полями (AddedAt, IsPrimary) — автоматичне M:N їх не зберегло б.</summary>
public class ProductCategory
{
    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public DateTime AddedAt { get; set; }
    public bool IsPrimary { get; set; }
}
