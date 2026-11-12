using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

/// <summary>Тонкий адаптер: без доступу до DbContext і без бізнес-правил.</summary>
[Route("api/products")]
public class ProductsController(IProductService service) : ApiControllerBase
{
    /// <summary>Список з пагінацією, фільтрацією (search, minPrice, maxPrice) і сортуванням (sortBy, sortDir).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetList([FromQuery] ProductQuery query, CancellationToken ct)
    {
        if (await ValidateAsync(query, ct) is { } invalid) return invalid;

        var page = await service.GetListAsync(query, ct);
        Response.Headers["X-Total-Count"] = page.TotalCount.ToString();
        Response.Headers["X-Page"] = page.Page.ToString();
        Response.Headers["X-Page-Size"] = page.PageSize.ToString();
        return Ok(page);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(long id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Картка товару з деталями, картинками й категоріями (Eager Loading: один запит).</summary>
    [HttpGet("{id:long}/details")]
    [ProducesResponseType<ProductDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> GetDetails(long id, CancellationToken ct)
        => Ok(await service.GetDetailsAsync(id, ct));

    /// <summary>Товари із залишком не більше порогу (друга специфікація).</summary>
    [HttpGet("low-stock")]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetLowStock([FromQuery] int threshold = 10, CancellationToken ct = default)
        => Ok(await service.GetLowStockAsync(threshold, ct));

    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto input, CancellationToken ct)
    {
        if (await ValidateAsync(input, ct) is { } invalid) return invalid;

        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}/price")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> ChangePrice(long id, ChangePriceDto input, CancellationToken ct)
    {
        if (await ValidateAsync(input, ct) is { } invalid) return invalid;
        return Ok(await service.ChangePriceAsync(id, input, ct));
    }

    [HttpPost("{id:long}/stock/add")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> AddStock(long id, StockChangeDto input, CancellationToken ct)
    {
        if (await ValidateAsync(input, ct) is { } invalid) return invalid;
        return Ok(await service.AddStockAsync(id, input.Quantity, ct));
    }

    /// <summary>Списати залишок; недостатній залишок -> 409.</summary>
    [HttpPost("{id:long}/stock/remove")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> RemoveStock(long id, StockChangeDto input, CancellationToken ct)
    {
        if (await ValidateAsync(input, ct) is { } invalid) return invalid;
        return Ok(await service.RemoveStockAsync(id, input.Quantity, ct));
    }

    [HttpPost("{id:long}/categories")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignCategory(long id, AssignCategoryDto input, CancellationToken ct)
    {
        await service.AssignCategoryAsync(id, input, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
