using Catalog.Bll.Dtos;

namespace Catalog.Bll.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    /// <summary>Explicit Loading: категорія + її товари (2 запити).</summary>
    Task<CategoryWithProductsDto> GetWithProductsAsync(int id, CancellationToken ct = default);
    /// <summary>M:N через проміжну сутність: товари, додані в категорію після вказаної дати.</summary>
    Task<IReadOnlyList<ProductDto>> GetRecentlyAddedAsync(string categoryName, DateTime? since, CancellationToken ct = default);
}
