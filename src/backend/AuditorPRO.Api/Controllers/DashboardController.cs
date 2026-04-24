using AuditorPRO.Application.Features.Dashboard;
using AuditorPRO.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;

    public DashboardController(IMediator mediator, AppDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), 200)]
    public async Task<IActionResult> Get([FromQuery] int? sociedadId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardQuery(sociedadId), ct);
        return Ok(result);
    }

    [HttpGet("puntos-control")]
    public async Task<IActionResult> GetPuntosControl(CancellationToken ct)
    {
        var controles = await _db.PuntosControl
            .AsNoTracking()
            .Select(p => new { p.Id, p.Codigo, p.Nombre, p.DominioId, p.Activo })
            .OrderBy(p => p.Codigo)
            .ToListAsync(ct);
        return Ok(new { total = controles.Count, controles });
    }
}
