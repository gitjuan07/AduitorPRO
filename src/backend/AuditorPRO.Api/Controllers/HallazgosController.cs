using AuditorPRO.Application.Common.Models;
using AuditorPRO.Application.Features.Hallazgos;
using AuditorPRO.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HallazgosController : ControllerBase
{
    private readonly IMediator _mediator;

    public HallazgosController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HallazgoDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] EstadoHallazgo? estado = null,
        [FromQuery] Criticidad? criticidad = null,
        [FromQuery] int? sociedadId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetHallazgosQuery(page, pageSize, estado, criticidad, sociedadId), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/plan-accion")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ActualizarPlanAccion(Guid id, [FromBody] ActualizarPlanAccionRequest request, CancellationToken ct)
    {
        await _mediator.Send(new ActualizarPlanAccionCommand(id, request.PlanAccion, request.FechaCompromiso, request.ResponsableEmail), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cerrar")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Cerrar(Guid id, [FromBody] CerrarRequest request, CancellationToken ct)
    {
        await _mediator.Send(new CerrarHallazgoCommand(id, request.Justificacion), ct);
        return NoContent();
    }
}

public record ActualizarPlanAccionRequest(string PlanAccion, DateOnly? FechaCompromiso, string? ResponsableEmail);
public record CerrarRequest(string Justificacion);
