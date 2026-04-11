using AuditorPRO.Application.Features.Cargas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "AuditorPRO.Admin,AuditorPRO.TI.Senior")]
public class CargasController : ControllerBase
{
    private readonly IMediator _mediator;
    public CargasController(IMediator mediator) => _mediator = mediator;

    [HttpPost("empleados")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<IActionResult> CargarEmpleados(
        [FromForm] IFormFile archivo,
        [FromForm] int sociedadId,
        CancellationToken ct)
    {
        var cmd = new CargarEmpleadosCommand(
            archivo.OpenReadStream(),
            archivo.FileName,
            archivo.ContentType,
            sociedadId
        );
        var resultado = await _mediator.Send(cmd, ct);
        return Ok(resultado);
    }

    [HttpPost("usuarios-sistema")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> CargarUsuariosSistema(
        [FromForm] IFormFile archivo,
        [FromForm] string sistema,
        CancellationToken ct)
    {
        var cmd = new CargarUsuariosSistemaCommand(
            archivo.OpenReadStream(),
            archivo.FileName,
            archivo.ContentType,
            sistema
        );
        var resultado = await _mediator.Send(cmd, ct);
        return Ok(resultado);
    }
}
