using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Reviews.WebApi.Controllers;

/// <summary>Базовий контролер: доступ до ISender (MediatR). Контролери лише передають команди й запити в MediatR.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
