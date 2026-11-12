using Catalog.Bll.Dtos;

namespace Catalog.Bll.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetListAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ProductDetailsDto> GetDetailsAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetLowStockAsync(int threshold, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct = default);
    Task<ProductDto> ChangePriceAsync(long id, ChangePriceDto input, CancellationToken ct = default);
    Task<ProductDto> AddStockAsync(long id, int quantity, CancellationToken ct = default);
    Task<ProductDto> RemoveStockAsync(long id, int quantity, CancellationToken ct = default);
    Task AssignCategoryAsync(long id, AssignCategoryDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
