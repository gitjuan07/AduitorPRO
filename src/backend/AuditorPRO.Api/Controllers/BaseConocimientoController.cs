using AuditorPRO.Application.Features.BaseConocimiento;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/base-conocimiento")]
[AllowAnonymous]
public class BaseConocimientoController : ControllerBase
{
    private readonly IMediator _mediator;
    public BaseConocimientoController(IMediator mediator) => _mediator = mediator;

    /// <summary>Lista documentos indexados con filtros y búsqueda</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? dominio,
        [FromQuery] string? busqueda,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(
            new GetBaseConocimientoQuery(dominio, busqueda, page, pageSize));
        return Ok(result);
    }

    /// <summary>Ingestir todos los archivos de un directorio del servidor</summary>
    [HttpPost("ingestir-directorio")]
    public async Task<IActionResult> IngestirDirectorio([FromBody] IngestirDirectorioRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RutaDirectorio))
            return BadRequest(new { error = "Debe indicar una ruta de directorio." });

        var usuario = User.FindFirst("preferred_username")?.Value ?? "demo";
        var resultado = await _mediator.Send(
            new IngestirDirectorioCommand(req.RutaDirectorio, usuario));
        return Ok(resultado);
    }

    /// <summary>Subir uno o varios archivos directamente desde el browser</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)] // 100MB máx total
    public async Task<IActionResult> Upload([FromForm] IFormFileCollection files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "No se recibieron archivos." });

        var usuario = User.FindFirst("preferred_username")?.Value ?? "demo";
        int procesados = 0, errores = 0;
        var detalles = new List<string>();

        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            var resultado = await _mediator.Send(
                new IngestirArchivoUploadCommand(stream, file.FileName, usuario));
            procesados += resultado.Procesados;
            errores    += resultado.Errores;
            detalles.AddRange(resultado.Detalles);
        }

        return Ok(new { procesados, errores, detalles });
    }

    /// <summary>Eliminar documento de la base de conocimiento</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var ok = await _mediator.Send(new EliminarBaseConocimientoCommand(id));
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Buscar contexto relevante (útil para IA)</summary>
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string q, [FromQuery] int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Parámetro 'q' requerido." });

        var contexto = await _mediator.Send(new BuscarContextoIAQuery(q, topK));
        return Ok(new { contexto });
    }
}

public record IngestirDirectorioRequest(string RutaDirectorio);
