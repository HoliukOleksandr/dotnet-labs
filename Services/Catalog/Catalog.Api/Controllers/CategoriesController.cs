using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[Route("api/categories")]
public class CategoriesController(ICategoryService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    /// <summary>Категорія з товарами (Explicit Loading: 2 запити).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryWithProductsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryWithProductsDto>> GetWithProducts(int id, CancellationToken ct)
        => Ok(await service.GetWithProductsAsync(id, ct));

    /// <summary>Товари, додані в категорію після дати since (за замовчуванням — останні 30 днів); M:N через AddedAt.</summary>
    [HttpGet("{name}/recent")]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetRecent(
        string name, [FromQuery] DateTime? since, CancellationToken ct)
        => Ok(await service.GetRecentlyAddedAsync(name, since, ct));
}
