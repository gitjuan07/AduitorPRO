using AuditorPRO.Application.Features.Conectores;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConectoresController : ControllerBase
{
    private readonly IMediator _mediator;
    public ConectoresController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "AuditorPRO.Admin,AuditorPRO.Auditor,AuditorPRO.TI.Senior")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetConectoresQuery(page, pageSize), ct));

    [HttpGet("{id:guid}/logs")]
    [Authorize(Roles = "AuditorPRO.Admin,AuditorPRO.Auditor,AuditorPRO.TI.Senior")]
    public async Task<IActionResult> GetLogs(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetConectorLogsQuery(id, page, pageSize), ct));

    [HttpPost]
    [Authorize(Roles = "AuditorPRO.Admin,AuditorPRO.TI.Senior")]
    public async Task<IActionResult> Crear([FromBody] CrearConectorCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "AuditorPRO.Admin,AuditorPRO.TI.Senior")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarConectorRequest body, CancellationToken ct)
    {
        var cmd = new ActualizarConectorCommand(id, body.Nombre, body.Descripcion,
            body.UrlEndpoint, body.AuthType, body.SecretKeyVaultRef, body.Estado, body.ConfiguracionJson);
        await _mediator.Send(cmd, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/probar")]
    [Authorize(Roles = "AuditorPRO.Admin,AuditorPRO.TI.Senior")]
    public async Task<IActionResult> Probar(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new ProbarConectorCommand(id), ct));
}

public record ActualizarConectorRequest(
    string Nombre,
    string? Descripcion,
    string? UrlEndpoint,
    string? AuthType,
    string? SecretKeyVaultRef,
    AuditorPRO.Domain.Enums.EstadoConector Estado,
    string? ConfiguracionJson
);
