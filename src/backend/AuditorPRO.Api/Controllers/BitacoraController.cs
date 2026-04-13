using AuditorPRO.Application.Features.Bitacora;
using AuditorPRO.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BitacoraController : ControllerBase
{
    private readonly IMediator _mediator;
    public BitacoraController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? usuarioId,
        [FromQuery] AccionBitacora? accion,
        [FromQuery] string? recurso,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBitacoraQuery(usuarioId, accion, recurso, desde, hasta, page, pageSize), ct));
}
