using Orders.Domain.Models;

namespace Orders.Dal.Abstractions;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);
    /// <summary>Атомарно зменшує залишок. NotFoundException — товару немає; BusinessConflictException — не вистачає.</summary>
    Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default);
}
