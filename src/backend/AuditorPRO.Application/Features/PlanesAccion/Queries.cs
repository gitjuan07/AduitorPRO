using AuditorPRO.Application.Common.Models;
using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.PlanesAccion;

public record GetPlanesAccionQuery(
    EstadoHallazgo? Estado = null,
    string? Responsable = null,
    bool? Vencidos = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<PlanAccionDto>>;

public class PlanAccionDto
{
    public Guid HallazgoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Criticidad { get; set; } = string.Empty;
    public string? Dominio { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? PlanAccion { get; set; }
    public string? Responsable { get; set; }
    public DateOnly? FechaCompromiso { get; set; }
    public DateOnly? FechaCierre { get; set; }
    public bool EsVencido { get; set; }
    public int DiasRestantes { get; set; }
}

public class GetPlanesAccionHandler : IRequestHandler<GetPlanesAccionQuery, PagedResult<PlanAccionDto>>
{
    private readonly IHallazgoRepository _repo;

    public GetPlanesAccionHandler(IHallazgoRepository repo) => _repo = repo;

    public async Task<PagedResult<PlanAccionDto>> Handle(GetPlanesAccionQuery request, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var all = (await _repo.FindAsync(h =>
            (request.Estado == null || h.Estado == request.Estado) &&
            (request.Responsable == null || h.ResponsableEmail!.Contains(request.Responsable)) &&
            (request.Vencidos == null || (request.Vencidos == true
                ? h.FechaCompromiso < hoy && h.Estado != EstadoHallazgo.CERRADO
                : h.FechaCompromiso >= hoy || h.Estado == EstadoHallazgo.CERRADO)),
            ct)).ToList();

        var total = all.Count;
        var items = all.OrderBy(h => h.FechaCompromiso)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(h =>
            {
                var dias = h.FechaCompromiso.HasValue
                    ? h.FechaCompromiso.Value.DayNumber - hoy.DayNumber
                    : 0;
                return new PlanAccionDto
                {
                    HallazgoId = h.Id,
                    Titulo = h.Titulo,
                    Criticidad = h.Criticidad.ToString(),
                    Estado = h.Estado.ToString(),
                    PlanAccion = h.PlanAccion,
                    Responsable = h.ResponsableEmail,
                    FechaCompromiso = h.FechaCompromiso,
                    FechaCierre = h.FechaCierre,
                    EsVencido = h.FechaCompromiso < hoy && h.Estado != EstadoHallazgo.CERRADO,
                    DiasRestantes = dias
                };
            });

        return new PagedResult<PlanAccionDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }
}
