using AuditorPRO.Application.Features.Evidencias;
using AuditorPRO.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EvidenciasController : ControllerBase
{
    private readonly IMediator _mediator;
    public EvidenciasController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? hallazgoId,
        [FromQuery] Guid? simulacionId,
        [FromQuery] TipoEvidencia? tipo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetEvidenciasQuery(hallazgoId, simulacionId, tipo, page, pageSize), ct));

    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Subir(
        [FromForm] IFormFile archivo,
        [FromForm] TipoEvidencia tipoEvidencia,
        [FromForm] string? descripcion,
        [FromForm] Guid? hallazgoId,
        [FromForm] Guid? simulacionId,
        [FromForm] int? sociedadId,
        [FromForm] string? periodoReferencia,
        CancellationToken ct)
    {
        var cmd = new SubirEvidenciaCommand(
            archivo.OpenReadStream(),
            archivo.FileName,
            archivo.ContentType,
            archivo.Length,
            tipoEvidencia,
            descripcion,
            hallazgoId,
            simulacionId,
            sociedadId,
            periodoReferencia
        );
        var id = await _mediator.Send(cmd, ct);
        return Ok(new { id });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new EliminarEvidenciaCommand(id), ct);
        return NoContent();
    }
}
