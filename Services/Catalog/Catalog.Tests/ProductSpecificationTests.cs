using Catalog.Bll.Dtos;
using Catalog.Bll.Specifications;
using Catalog.Domain.Entities;

namespace Catalog.Tests;

/// <summary>Специфікації не залежать від EF, тому перевіряються на звичайному списку в пам'яті.</summary>
public class ProductSpecificationTests
{
    private static List<Product> Products()
    {
        var list = new List<Product>();
        for (var i = 1; i <= 7; i++)
        {
            var p = new Product($"Item {i}", $"SKU-{i}", i * 10m);
            p.AddStock(i);
            list.Add(p);
        }
        return list;
    }

    [Fact]
    public void Filter_spec_returns_requested_page_sorted_by_price()
    {
        var query = new ProductQuery(Page: 2, PageSize: 3, SortBy: "price", SortDir: "asc");

        var page = new ProductsFilterSpec(query, 2, 3).Evaluate(Products()).ToList();

        Assert.Equal(new[] { "Item 4", "Item 5", "Item 6" }, page.Select(p => p.Name));
    }

    [Fact]
    public void Count_spec_ignores_paging_and_applies_filters()
    {
        var query = new ProductQuery(Page: 2, PageSize: 3, MinPrice: 30m, MaxPrice: 60m);

        var count = new ProductsCountSpec(query).Evaluate(Products()).Count();

        Assert.Equal(4, count);     // ціни 30, 40, 50, 60
    }

    [Fact]
    public void Second_spec_with_other_criteria_works_independently()
    {
        var low = new LowStockProductsSpec(3).Evaluate(Products()).ToList();

        Assert.Equal(new[] { "Item 1", "Item 2", "Item 3" }, low.Select(p => p.Name));
    }
}
