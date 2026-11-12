using Orders.Domain.Models;

namespace Orders.Dal.Abstractions;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderSummary>> GetPagedByCustomerAsync(
        long customerId, string? status, int page, int pageSize, CancellationToken ct = default);
    /// <summary>usp_ChangeOrderStatus: транзакційна зміна статусу з перевіркою допустимого переходу.</summary>
    Task ChangeStatusAsync(long orderId, string newStatus, string? changedBy, CancellationToken ct = default);
    /// <summary>Перехід у StockConfirmed (через usp_ChangeOrderStatus).</summary>
    Task ConfirmAsync(long orderId, CancellationToken ct = default);
}
