using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Orders.Bll.Mapping;
using Orders.Bll.Services;
using Orders.Dal.Abstractions;
using Orders.Domain.Constants;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Tests;

public class OrderServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _uow.Orders.Returns(_orders);
        _uow.Products.Returns(_products);

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>(), NullLoggerFactory.Instance)
            .CreateMapper();
        _service = new OrderService(_uow, mapper, NullLogger<OrderService>.Instance);
    }

    [Fact]
    public async Task ConfirmOrder_for_unknown_order_throws_NotFound_and_rolls_back()
    {
        _orders.GetWithItemsAsync(42, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Order?>(null));

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ConfirmOrderAsync(42));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmOrder_rolls_back_when_second_repository_fails()
    {
        var order = new Order { Id = 1, CustomerId = 1, Status = OrderStatus.Submitted };
        order.Items.Add(new OrderItem { OrderId = 1, ProductId = 7, ProductName = "X", UnitPrice = 10, Units = 100 });
        _orders.GetWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Order?>(order));
        _products.DecreaseStockAsync(7, 100, Arg.Any<CancellationToken>())
                 .ThrowsAsync(new BusinessConflictException("Not enough stock"));

        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.ConfirmOrderAsync(1));

        // статус вже змінювали в першому репозиторії — транзакція обов'язково відкочується
        await _orders.Received(1).ConfirmAsync(1, Arg.Any<CancellationToken>());
        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_with_unknown_status_throws_Validation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.ChangeStatusAsync(1, "Flying"));
    }
}
