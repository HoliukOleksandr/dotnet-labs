using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>1:1 з Product: первинний ключ є водночас зовнішнім.</summary>
public class ProductDetail
{
    private ProductDetail() { }                             // для EF Core

    public ProductDetail(string description, string? manufacturer, int? weightGrams, int warrantyMonths = 12)
    {
        if (weightGrams is <= 0) throw new DomainException("Weight must be positive");
        if (warrantyMonths < 0) throw new DomainException("Warranty cannot be negative");
        Description = description ?? "";
        Manufacturer = manufacturer;
        WeightGrams = weightGrams;
        WarrantyMonths = warrantyMonths;
    }

    public long ProductId { get; private set; }
    public string Description { get; private set; } = "";
    public string? Manufacturer { get; private set; }
    public int? WeightGrams { get; private set; }
    public int WarrantyMonths { get; private set; }
}
