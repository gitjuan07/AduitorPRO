using AuditorPRO.Application.Common.Models;
using AuditorPRO.Application.Features.Simulaciones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SimulacionesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SimulacionesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SimulacionListDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSimulacionesQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SimulacionDetalleDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSimulacionDetalleQuery(id), ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Guid), 201)]
    public async Task<IActionResult> Iniciar([FromBody] IniciarSimulacionCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/cancelar")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CancelarSimulacionCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/ejecutar")]
    [Authorize]
    [ProducesResponseType(typeof(EjecutarSimulacionResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Ejecutar(Guid id, CancellationToken ct)
    {
        try
        {
            var resultado = await _mediator.Send(new EjecutarSimulacionCommand(id), ct);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("todas")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> BorrarTodas(CancellationToken ct)
    {
        try
        {
            var borradas = await _mediator.Send(new BorrarTodasSimulacionesCommand(), ct);
            return Ok(new { borradas });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, detalle = ex.InnerException?.Message });
        }
    }

    [HttpPost("{id:guid}/ejecutar-control-cruzado")]
    [Authorize]
    [ProducesResponseType(typeof(ControlCruzadoResultado), 200)]
    public async Task<IActionResult> EjecutarControlCruzado(
        Guid id,
        [FromBody] EjecutarControlCruzadoRequest body,
        CancellationToken ct)
    {
        try
        {
            var cmd = new EjecutarControlCruzadoCommand(
                id,
                body.Objetivo,
                body.TipoControlCruzado ?? "COMPLETO"
            );
            var resultado = await _mediator.Send(cmd, ct);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}

public record EjecutarControlCruzadoRequest(
    string? Objetivo,
    string? TipoControlCruzado
);
