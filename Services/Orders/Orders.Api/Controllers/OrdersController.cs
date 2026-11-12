using Microsoft.AspNetCore.Mvc;
using Orders.Bll.Dtos;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

/// <summary>Тонкий адаптер: без SQL і бізнес-правил, лише HTTP-контракт.</summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController(IOrderService orders) : ControllerBase
{
    /// <summary>Замовлення з деталями та позиціями.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(long id, CancellationToken ct)
        => Ok(await orders.GetByIdAsync(id, ct));

    /// <summary>Замовлення клієнта (фільтр за статусом, пагінація).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetByCustomer(
        [FromQuery] long customerId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        => Ok(await orders.GetByCustomerAsync(customerId, status, page, pageSize, ct));

    /// <summary>Створити замовлення (знімок назви й ціни товарів береться з Products).</summary>
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderDto input, CancellationToken ct)
    {
        var created = await orders.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Змінити адресу доставки та коментар.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> UpdateDetails(long id, UpdateOrderDetailsDto input, CancellationToken ct)
        => Ok(await orders.UpdateDetailsAsync(id, input, ct));

    /// <summary>М'яке видалення (лише Submitted/Cancelled).</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await orders.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Підтвердити замовлення: статус + зменшення залишків в одній транзакції.</summary>
    [HttpPost("{id:long}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        await orders.ConfirmOrderAsync(id, ct);
        return NoContent();
    }

    /// <summary>Змінити статус (Paid, Shipped, Cancelled ...); недопустимий перехід -> 409.</summary>
    [HttpPost("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(long id, ChangeStatusDto input, CancellationToken ct)
    {
        await orders.ChangeStatusAsync(id, input.Status, ct);
        return NoContent();
    }
}
