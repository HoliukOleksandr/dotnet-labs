using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Abstractions;
using Orders.Domain.Constants;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class OrderRepository : BaseDapperRepository<Order>, IOrderRepository
{
    public OrderRepository(SqlConnection c, Func<SqlTransaction?> tx) : base(c, tx, "dbo.Orders") { }

    protected override string ActiveFilter => " AND IsDeleted = 0";

    /// <summary>Замовлення з деталями (1:1) і позиціями (1:N) одним запитом — multi-mapping.</summary>
    public Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default) => GuardAsync<Order?>(async () =>
    {
        const string sql = @"
            SELECT o.Id, o.CustomerId, o.Status, o.OrderDate, d.ShippingAddress, d.Comment,
                   i.OrderId, i.ProductId, i.ProductName, i.UnitPrice, i.Discount, i.Units
            FROM dbo.Orders o
            JOIN dbo.OrderDetails d ON d.OrderId = o.Id
            LEFT JOIN dbo.OrderItems i ON i.OrderId = o.Id
            WHERE o.Id = @OrderId AND o.IsDeleted = 0";

        await EnsureOpenAsync(ct);
        Order? result = null;
        await Connection.QueryAsync<Order, OrderItem?, Order>(
            Cmd(sql, new { OrderId = orderId }, ct: ct),
            (order, item) =>                       // кожен рядок JOIN: Order + одна позиція
            {
                result ??= order;
                if (item is not null) result.Items.Add(item);
                return result;
            },
            splitOn: "OrderId");                   // з цього стовпця починається OrderItem
        return result;
    });

    /// <summary>Список замовлень клієнта: usp_GetCustomerOrders (фільтр за статусом + пагінація).</summary>
    public Task<IReadOnlyList<OrderSummary>> GetPagedByCustomerAsync(
        long customerId, string? status, int page, int pageSize, CancellationToken ct = default)
        => GuardAsync<IReadOnlyList<OrderSummary>>(async () =>
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<OrderSummary>(Cmd(
            "dbo.usp_GetCustomerOrders",
            new { CustomerId = customerId, Status = status, PageNumber = page, PageSize = pageSize },
            CommandType.StoredProcedure, ct));
        return rows.ToList();
    });

    /// <summary>Створення через usp_CreateOrder (TVP dbo.OrderItemList): Order + Details + Items + історія.</summary>
    public override Task<long> AddAsync(Order order, CancellationToken ct = default) => GuardAsync<long>(async () =>
    {
        var items = new DataTable();
        items.Columns.Add("ProductId", typeof(long));
        items.Columns.Add("Units", typeof(int));
        foreach (var item in order.Items) items.Rows.Add(item.ProductId, item.Units);

        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleAsync<long>(Cmd(
            "dbo.usp_CreateOrder",
            new
            {
                order.CustomerId,
                order.ShippingAddress,
                order.Comment,
                Items = items.AsTableValuedParameter("dbo.OrderItemList"),
                CreatedBy = "api"
            },
            CommandType.StoredProcedure, ct));
    });

    /// <summary>Оновлює лише деталі замовлення (адреса, коментар) та службові поля.</summary>
    public override Task<bool> UpdateAsync(Order order, CancellationToken ct = default) => GuardAsync<bool>(async () =>
    {
        const string sql = @"
            UPDATE d
            SET d.ShippingAddress = @ShippingAddress, d.Comment = @Comment
            FROM dbo.OrderDetails d
            JOIN dbo.Orders o ON o.Id = d.OrderId
            WHERE d.OrderId = @Id AND o.IsDeleted = 0;

            UPDATE dbo.Orders
            SET UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0;";
        await EnsureOpenAsync(ct);
        var rows = await Connection.ExecuteAsync(Cmd(sql,
            new { order.Id, order.ShippingAddress, order.Comment, UpdatedBy = "api" }, ct: ct));
        return rows > 0;
    });

    /// <summary>М'яке видалення через usp_DeleteOrder (дозволено лише Submitted/Cancelled).</summary>
    public override Task<bool> DeleteAsync(long id, CancellationToken ct = default) => GuardAsync<bool>(async () =>
    {
        await EnsureOpenAsync(ct);
        await Connection.ExecuteAsync(Cmd("dbo.usp_DeleteOrder",
            new { OrderId = id, DeletedBy = "api" }, CommandType.StoredProcedure, ct));
        return true;   // якщо замовлення немає, процедура кидає 50001 -> NotFoundException
    });

    public Task ChangeStatusAsync(long orderId, string newStatus, string? changedBy, CancellationToken ct = default)
        => GuardAsync(async () =>
    {
        await EnsureOpenAsync(ct);
        await Connection.ExecuteAsync(Cmd("dbo.usp_ChangeOrderStatus",
            new { OrderId = orderId, NewStatus = newStatus, ChangedBy = changedBy },
            CommandType.StoredProcedure, ct));
    });

    public Task ConfirmAsync(long orderId, CancellationToken ct = default)
        => ChangeStatusAsync(orderId, OrderStatus.StockConfirmed, "api", ct);
}
