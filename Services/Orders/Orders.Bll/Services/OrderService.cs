using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Orders.Bll.Dtos;
using Orders.Dal.Abstractions;
using Orders.Domain.Constants;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Bll.Services;

/// <summary>Єдиний власник бізнес-логіки замовлень; контролери лише викликають ці методи.</summary>
public sealed class OrderService(IUnitOfWork uow, IMapper mapper, ILogger<OrderService> logger) : IOrderService
{
    private const string Actor = "api";

    public async Task<OrderDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var order = await uow.Orders.GetWithItemsAsync(id, ct)
                    ?? throw new NotFoundException($"Order {id} not found");
        return mapper.Map<OrderDto>(order);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetByCustomerAsync(
        long customerId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        if (customerId < 1) throw new ValidationException("customerId must be >= 1");
        if (status is not null && !OrderStatus.IsValid(status))
            throw new ValidationException($"Unknown status '{status}'");
        CustomerService.ValidatePaging(page, pageSize);

        var rows = await uow.Orders.GetPagedByCustomerAsync(customerId, status, page, pageSize, ct);
        return new PagedResult<OrderSummaryDto>(mapper.Map<List<OrderSummaryDto>>(rows), page, pageSize, null);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct = default)
    {
        // --- бізнес-валідація (потрібен стан із БД) ---
        if (!await uow.Customers.ExistsAsync(input.CustomerId, ct))
            throw new NotFoundException($"Customer {input.CustomerId} not found");

        // однакові товари об'єднуємо в одну позицію
        var merged = input.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(x => x.Units) })
            .ToList();

        var products = await uow.Products.GetByIdsAsync(merged.Select(m => m.ProductId).ToList(), ct);

        var order = new Order
        {
            CustomerId = input.CustomerId,
            ShippingAddress = input.ShippingAddress,
            Comment = input.Comment
        };

        foreach (var line in merged)
        {
            var product = products.FirstOrDefault(p => p.Id == line.ProductId)
                          ?? throw new NotFoundException($"Product {line.ProductId} not found");
            if (product.Stock < line.Units)
                throw new BusinessConflictException(
                    $"Not enough stock for product {product.Id}: requested {line.Units}, available {product.Stock}");

            // знімок назви й ціни на момент покупки (політика дублювання з Лаб. 01);
            // процедура usp_CreateOrder записує той самий знімок з таблиці Products
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Units = line.Units
            });
        }

        var id = await uow.Orders.AddAsync(order, ct);
        logger.LogInformation("Order {OrderId} created for customer {CustomerId}", id, input.CustomerId);
        return await GetByIdAsync(id, ct);
    }

    public async Task<OrderDto> UpdateDetailsAsync(long id, UpdateOrderDetailsDto input, CancellationToken ct = default)
    {
        var current = await uow.Orders.GetWithItemsAsync(id, ct)
                      ?? throw new NotFoundException($"Order {id} not found");
        if (current.Status is OrderStatus.Shipped or OrderStatus.Cancelled)
            throw new BusinessConflictException($"Order in status {current.Status} cannot be edited");

        var update = new Order { Id = id, ShippingAddress = input.ShippingAddress, Comment = input.Comment };
        if (!await uow.Orders.UpdateAsync(update, ct))
            throw new NotFoundException($"Order {id} not found");
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        // usp_DeleteOrder: 50001 -> 404, 50004 (не Submitted/Cancelled) -> 409
        await uow.Orders.DeleteAsync(id, ct);
    }

    public async Task ChangeStatusAsync(long orderId, string newStatus, CancellationToken ct = default)
    {
        if (!OrderStatus.IsValid(newStatus))
            throw new ValidationException($"Unknown status '{newStatus}'");
        // допустимість переходу перевіряє транзакційна процедура (недопустимий -> 409)
        await uow.Orders.ChangeStatusAsync(orderId, newStatus, Actor, ct);
    }

    public async Task ConfirmOrderAsync(long orderId, CancellationToken ct = default)
    {
        // ReadCommitted: без «брудних» читань і з мінімумом блокувань (компроміс deadlocks vs consistency)
        await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var order = await uow.Orders.GetWithItemsAsync(orderId, ct)
                        ?? throw new NotFoundException($"Order {orderId} not found");
            if (order.Items.Count == 0)
                throw new BusinessConflictException($"Order {orderId} has no items");

            // Submitted -> AwaitingValidation -> StockConfirmed (кожен перехід перевіряє процедура)
            if (order.Status == OrderStatus.Submitted)
                await uow.Orders.ChangeStatusAsync(orderId, OrderStatus.AwaitingValidation, Actor, ct);
            await uow.Orders.ConfirmAsync(orderId, ct);

            // брак товару -> BusinessConflictException -> відкат і статусу
            foreach (var item in order.Items)
                await uow.Products.DecreaseStockAsync(item.ProductId, item.Units, ct);

            await uow.CommitAsync(ct);
            logger.LogInformation("Order {OrderId} confirmed", orderId);
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);   // статус повертається назад
            throw;
        }
    }
}
