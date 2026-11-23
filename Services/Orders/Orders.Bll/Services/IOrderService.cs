using Orders.Bll.Dtos;

namespace Orders.Bll.Services;

public interface IOrderService
{
    Task<OrderDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PagedResult<OrderSummaryDto>> GetByCustomerAsync(long customerId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct = default);
    Task<OrderDto> UpdateDetailsAsync(long id, UpdateOrderDetailsDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    /// <summary>Транзакційна операція: статус -> StockConfirmed + зменшення залишків товарів.</summary>
    Task ConfirmOrderAsync(long orderId, CancellationToken ct = default);
    Task ChangeStatusAsync(long orderId, string newStatus, CancellationToken ct = default);
}
