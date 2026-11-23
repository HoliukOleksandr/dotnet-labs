using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Bll.Specifications;
using Catalog.Dal.Abstractions;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Catalog.Bll.Services;

/// <summary>Сервіс не працює з EF напряму — лише через репозиторії та Unit of Work.</summary>
public sealed class ProductService(IUnitOfWork uow, IMapper mapper, ILogger<ProductService> logger) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetListAsync(ProductQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // totalCount рахує БД за тими самими критеріями, але без Skip/Take
        var total = await uow.Products.CountAsync(new ProductsCountSpec(query), ct);
        var items = await uow.Products.ListAsync(new ProductsFilterSpec(query, page, pageSize), ct);

        return new PagedResult<ProductDto>(mapper.Map<List<ProductDto>>(items), total, page, pageSize);
    }

    public async Task<ProductDto> GetByIdAsync(long id, CancellationToken ct = default)
        => mapper.Map<ProductDto>(await GetOrThrowAsync(id, ct));

    public async Task<ProductDetailsDto> GetDetailsAsync(long id, CancellationToken ct = default)
    {
        var product = await uow.Products.GetWithDetailsAsync(id, ct)      // Eager Loading
                      ?? throw new EntityNotFoundException($"Product {id} not found");
        return mapper.Map<ProductDetailsDto>(product);
    }

    public async Task<IReadOnlyList<ProductDto>> GetLowStockAsync(int threshold, CancellationToken ct = default)
    {
        var items = await uow.Products.ListAsync(new LowStockProductsSpec(threshold), ct);
        return mapper.Map<List<ProductDto>>(items);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct = default)
    {
        // правило, якому потрібні дані з БД, перевіряє сервіс; правила сутності — сама сутність
        if (await uow.Products.ExistsBySkuAsync(input.Sku, ct))
            throw new ConflictException($"Product with SKU '{input.Sku}' already exists");

        var product = new Product(input.Name, input.Sku, input.Price);
        await uow.Products.AddAsync(product, ct);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Product {ProductId} created with SKU {Sku}", product.Id, product.Sku);
        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> ChangePriceAsync(long id, ChangePriceDto input, CancellationToken ct = default)
    {
        var product = await GetOrThrowAsync(id, ct);
        product.ChangePrice(input.Price);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> AddStockAsync(long id, int quantity, CancellationToken ct = default)
    {
        var product = await GetOrThrowAsync(id, ct);
        product.AddStock(quantity);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> RemoveStockAsync(long id, int quantity, CancellationToken ct = default)
    {
        var product = await GetOrThrowAsync(id, ct);
        product.RemoveStock(quantity);                // недостатній залишок -> DomainException -> 409
        await uow.SaveChangesAsync(ct);
        return mapper.Map<ProductDto>(product);
    }

    public async Task AssignCategoryAsync(long id, AssignCategoryDto input, CancellationToken ct = default)
    {
        _ = await uow.Products.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException($"Product {id} not found");
        var category = await uow.Categories.GetByIdAsync(input.CategoryId, ct)
                       ?? throw new EntityNotFoundException($"Category {input.CategoryId} not found");

        if (await uow.ProductCategories.GetByIdAsync((id, input.CategoryId), ct) is not null)
            throw new ConflictException($"Product {id} is already in category '{category.Name}'");
        if (input.IsPrimary && await uow.ProductCategories.HasPrimaryAsync(id, ct))
            throw new ConflictException($"Product {id} already has a primary category");

        await uow.ProductCategories.AddAsync(new ProductCategory
        {
            ProductId = id,
            CategoryId = category.Id,
            AddedAt = DateTime.UtcNow,
            IsPrimary = input.IsPrimary
        }, ct);
        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var product = await GetOrThrowAsync(id, ct);
        uow.Products.Delete(product);
        await uow.SaveChangesAsync(ct);
    }

    private async Task<Product> GetOrThrowAsync(long id, CancellationToken ct)
        => await uow.Products.GetByIdAsync(id, ct)
           ?? throw new EntityNotFoundException($"Product {id} not found");
}
