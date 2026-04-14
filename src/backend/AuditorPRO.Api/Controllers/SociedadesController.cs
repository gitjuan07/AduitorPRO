using AuditorPRO.Application.Features.Sociedades;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SociedadesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SociedadesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Lista todas las sociedades. Usa ?soloActivas=true para filtrar inactivas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SociedadDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] bool? soloActivas, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSociedadesQuery(soloActivas), ct);
        return Ok(result);
    }

    /// <summary>Obtiene una sociedad por su código SAP (ej: CR01, PA03).</summary>
    [HttpGet("{codigo}")]
    [ProducesResponseType(typeof(SociedadDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByCodigo(string codigo, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSociedadByCodigoQuery(codigo.ToUpperInvariant()), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
