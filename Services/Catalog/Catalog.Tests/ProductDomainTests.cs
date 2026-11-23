using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Tests;

public class ProductDomainTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_rejects_non_positive_price(decimal price)
        => Assert.Throws<DomainException>(() => new Product("Mouse", "MS-1", price));

    [Fact]
    public void RemoveStock_decreases_available_stock()
    {
        var product = new Product("Mouse", "MS-1", 100m);
        product.AddStock(10);

        product.RemoveStock(4);

        Assert.Equal(6, product.AvailableStock);
    }

    [Fact]
    public void RemoveStock_more_than_available_throws_and_keeps_stock()
    {
        var product = new Product("Mouse", "MS-1", 100m);
        product.AddStock(5);

        Assert.Throws<DomainException>(() => product.RemoveStock(6));

        Assert.Equal(5, product.AvailableStock);
    }

    [Fact]
    public void AssignCategory_twice_throws()
    {
        var product = new Product("Mouse", "MS-1", 100m);
        var category = new Category("Peripherals", "peripherals");
        product.AssignCategory(category, isPrimary: true);

        Assert.Throws<DomainException>(() => product.AssignCategory(category));
    }
}
