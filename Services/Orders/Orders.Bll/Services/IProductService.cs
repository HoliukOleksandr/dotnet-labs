using Orders.Bll.Dtos;

namespace Orders.Bll.Services;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);
}
