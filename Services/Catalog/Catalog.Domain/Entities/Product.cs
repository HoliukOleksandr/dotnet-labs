using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>
/// Корінь агрегату «Товар» (аналог CatalogItem з eShop). Бізнес-правила — у методах сутності,
/// а не у відкритих setter-ах: ціна задається в конструкторі й перевіряється, залишок змінюється лише методами.
/// </summary>
public class Product
{
    private Product() { }                                   // для EF Core

    public Product(string name, string sku, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required");
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("SKU is required");
        if (price <= 0) throw new DomainException("Price must be positive");
        Name = name.Trim();
        Sku = sku.Trim();
        Price = price;
        CreatedAt = DateTime.UtcNow;
    }

    public long Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Sku { get; private set; } = "";
    public decimal Price { get; private set; }
    public int AvailableStock { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();   // оптимістична конкурентність

    public ProductDetail? Detail { get; private set; }                  // 1:1
    public List<ProductImage> Images { get; } = new();                  // 1:N
    public List<ProductCategory> ProductCategories { get; } = new();    // M:N через проміжну сутність

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required");
        Name = name.Trim();
        Touch();
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0) throw new DomainException("Price must be positive");
        Price = newPrice;
        Touch();
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive");
        AvailableStock += quantity;
        Touch();
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive");
        if (AvailableStock < quantity) throw new DomainException($"Only {AvailableStock} units available");
        AvailableStock -= quantity;
        Touch();
    }

    public void SetDetail(ProductDetail detail)
    {
        Detail = detail ?? throw new DomainException("Detail is required");
        Touch();
    }

    public void AddImage(string url, int sortOrder = 0, bool isMain = false)
    {
        if (isMain)
            foreach (var image in Images) image.SetMain(false);     // головна картинка лише одна
        Images.Add(new ProductImage(url, sortOrder, isMain));
        Touch();
    }

    public void AssignCategory(Category category, bool isPrimary = false)
    {
        if (ProductCategories.Any(pc => ReferenceEquals(pc.Category, category) ||
                                        (category.Id != 0 && pc.CategoryId == category.Id)))
            throw new DomainException($"Product is already in category '{category.Name}'");
        if (isPrimary)
            foreach (var pc in ProductCategories) pc.IsPrimary = false;   // основна категорія лише одна

        ProductCategories.Add(new ProductCategory
        {
            Product = this,
            Category = category,
            AddedAt = DateTime.UtcNow,
            IsPrimary = isPrimary
        });
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
