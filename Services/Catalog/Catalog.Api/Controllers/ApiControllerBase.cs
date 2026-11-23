using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Явна валідація FluentValidation (AddFluentValidation застарів). Якщо є помилки — готова
    /// відповідь 400 у форматі ValidationProblemDetails з переліком помилок по полях, інакше null.
    /// </summary>
    protected async Task<ActionResult?> ValidateAsync<T>(T input, CancellationToken ct)
    {
        var validator = HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null) return null;

        var result = await validator.ValidateAsync(input, ct);
        return result.IsValid
            ? null
            : ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
    }
}
