using AuditorPRO.Application.Features.PlanesAccion;
using AuditorPRO.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/planes-accion")]
[Authorize]
public class PlanesAccionController : ControllerBase
{
    private readonly IMediator _mediator;
    public PlanesAccionController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] EstadoHallazgo? estado,
        [FromQuery] string? responsable,
        [FromQuery] bool? vencidos,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetPlanesAccionQuery(estado, responsable, vencidos, page, pageSize), ct));

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearPlanAccionCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return Ok(new { id });
    }

    [HttpPut("{hallazgoId:guid}/estatus")]
    public async Task<IActionResult> ActualizarEstatus(Guid hallazgoId, [FromBody] ActualizarEstatusRequest body, CancellationToken ct)
    {
        await _mediator.Send(new ActualizarEstatusAccionCommand(hallazgoId, body.Estado, body.Comentario, body.PorcentajeAvance), ct);
        return NoContent();
    }
}

public record ActualizarEstatusRequest(EstadoHallazgo Estado, string? Comentario, int? PorcentajeAvance);
