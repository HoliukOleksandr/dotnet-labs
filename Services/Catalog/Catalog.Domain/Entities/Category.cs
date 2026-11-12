using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Category
{
    private Category() { }                                  // для EF Core

    public Category(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Category name is required");
        if (string.IsNullOrWhiteSpace(slug)) throw new DomainException("Category slug is required");
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public List<ProductCategory> ProductCategories { get; } = new();
}
