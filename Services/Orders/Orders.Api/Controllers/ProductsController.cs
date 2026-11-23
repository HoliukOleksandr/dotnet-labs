using Microsoft.AspNetCore.Mvc;
using Orders.Bll.Dtos;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

/// <summary>Локальна копія довідника Каталогу: сервіс замовлень лише читає товари.</summary>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken ct)
        => Ok(await products.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(long id, CancellationToken ct)
        => Ok(await products.GetByIdAsync(id, ct));
}
