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
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetConectoresQuery(page, pageSize), ct));

    [HttpGet("{id:guid}/logs")]
    [Authorize]
    public async Task<IActionResult> GetLogs(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetConectorLogsQuery(id, page, pageSize), ct));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Crear([FromBody] CrearConectorCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarConectorRequest body, CancellationToken ct)
    {
        var cmd = new ActualizarConectorCommand(id, body.Nombre, body.Sistema, body.Descripcion,
            body.TipoConexion, body.UrlEndpoint, body.AuthType, body.SecretKeyVaultRef, body.Estado, body.ConfiguracionJson);
        await _mediator.Send(cmd, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/probar-query")]
    [Authorize]
    public async Task<IActionResult> ProbarQuery(Guid id, [FromBody] ProbarQueryRequest? body, CancellationToken ct)
        => Ok(await _mediator.Send(new ProbarQueryCommand(id, body?.ConfiguracionJsonOverride), ct));

    [HttpPost("{id:guid}/probar")]
    [Authorize]
    public async Task<IActionResult> Probar(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new ProbarConectorCommand(id), ct));

    [HttpPost("{id:guid}/ejecutar")]
    [Authorize]
    public async Task<IActionResult> Ejecutar(Guid id, [FromQuery] int maxFilas = 500, CancellationToken ct = default)
        => Ok(await _mediator.Send(new EjecutarConectorCommand(id, maxFilas), ct));

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new EliminarConectorCommand(id), ct);
        return NoContent();
    }
}

public record ActualizarConectorRequest(
    string Nombre,
    string Sistema,
    string? Descripcion,
    AuditorPRO.Domain.Enums.TipoConector TipoConexion,
    string? UrlEndpoint,
    string? AuthType,
    string? SecretKeyVaultRef,
    AuditorPRO.Domain.Enums.EstadoConector Estado,
    string? ConfiguracionJson
);

public record ProbarQueryRequest(string? ConfiguracionJsonOverride);
