using AuditorPRO.Application.Common.Models;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.Hallazgos;

public record GetHallazgosQuery(
    int Page = 1,
    int PageSize = 50,
    EstadoHallazgo? Estado = null,
    Criticidad? Criticidad = null,
    int? SociedadId = null,
    Guid? SimulacionId = null,
    string? TipoHallazgo = null
) : IRequest<PagedResult<HallazgoDto>>;

public class HallazgoDto
{
    public Guid Id { get; set; }
    public Guid? SimulacionId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Criticidad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? TipoHallazgo { get; set; }
    public string? NormaAfectada { get; set; }
    public string? RiesgoAsociado { get; set; }
    public string? ResponsableEmail { get; set; }
    public DateOnly? FechaCompromiso { get; set; }
    public DateOnly? FechaCierre { get; set; }
    public string? PlanAccion { get; set; }
    public string? Sociedad { get; set; }
    public string? UsuarioSAP { get; set; }
    public string? Cedula { get; set; }
    public string? RolAfectado { get; set; }
    public string? TransaccionesAfectadas { get; set; }
    public string? CasoSESuiteRef { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetHallazgosHandler : IRequestHandler<GetHallazgosQuery, PagedResult<HallazgoDto>>
{
    private readonly IHallazgoRepository _repo;

    public GetHallazgosHandler(IHallazgoRepository repo) => _repo = repo;

    public async Task<PagedResult<HallazgoDto>> Handle(GetHallazgosQuery request, CancellationToken cancellationToken)
    {
        var all = (await _repo.GetAllAsync(cancellationToken)).ToList();

        if (request.Estado.HasValue)
            all = all.Where(h => h.Estado == request.Estado.Value).ToList();
        if (request.Criticidad.HasValue)
            all = all.Where(h => h.Criticidad == request.Criticidad.Value).ToList();
        if (request.SociedadId.HasValue)
            all = all.Where(h => h.SociedadId == request.SociedadId.Value).ToList();
        if (request.SimulacionId.HasValue)
            all = all.Where(h => h.SimulacionId == request.SimulacionId.Value).ToList();
        if (!string.IsNullOrWhiteSpace(request.TipoHallazgo))
            all = all.Where(h => h.TipoHallazgo == request.TipoHallazgo).ToList();

        var total = all.Count;
        var items = all
            .OrderByDescending(h => h.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(h => new HallazgoDto
            {
                Id                    = h.Id,
                SimulacionId          = h.SimulacionId,
                Titulo                = h.Titulo,
                Descripcion           = h.Descripcion,
                Criticidad            = h.Criticidad.ToString(),
                Estado                = h.Estado.ToString(),
                TipoHallazgo          = h.TipoHallazgo,
                NormaAfectada         = h.NormaAfectada,
                RiesgoAsociado        = h.RiesgoAsociado,
                ResponsableEmail      = h.ResponsableEmail,
                FechaCompromiso       = h.FechaCompromiso,
                FechaCierre           = h.FechaCierre,
                PlanAccion            = h.PlanAccion,
                UsuarioSAP            = h.UsuarioSAP,
                Cedula                = h.Cedula,
                RolAfectado           = h.RolAfectado,
                TransaccionesAfectadas = h.TransaccionesAfectadas,
                CasoSESuiteRef        = h.CasoSESuiteRef,
                CreatedAt             = h.CreatedAt
            });

        return new PagedResult<HallazgoDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
