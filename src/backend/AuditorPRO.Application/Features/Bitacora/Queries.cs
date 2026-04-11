using AuditorPRO.Application.Common.Models;
using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.Bitacora;

public record GetBitacoraQuery(
    string? UsuarioId = null,
    AccionBitacora? Accion = null,
    string? Recurso = null,
    DateTime? Desde = null,
    DateTime? Hasta = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<BitacoraEventoDto>>;

public class BitacoraEventoDto
{
    public Guid Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string? UsuarioEmail { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Recurso { get; set; }
    public string? RecursoId { get; set; }
    public string? Descripcion { get; set; }
    public string? DatosAntes { get; set; }
    public string? DatosDespues { get; set; }
    public string? IpOrigen { get; set; }
    public bool Exitoso { get; set; }
    public DateTime OcurridoAt { get; set; }
}

public class GetBitacoraHandler : IRequestHandler<GetBitacoraQuery, PagedResult<BitacoraEventoDto>>
{
    private readonly IRepository<BitacoraEvento> _repo;

    public GetBitacoraHandler(IRepository<BitacoraEvento> repo) => _repo = repo;

    public async Task<PagedResult<BitacoraEventoDto>> Handle(GetBitacoraQuery request, CancellationToken ct)
    {
        var desde = request.Desde ?? DateTime.UtcNow.AddDays(-30);
        var hasta = request.Hasta ?? DateTime.UtcNow;

        var all = (await _repo.FindAsync(e =>
            (request.UsuarioId == null || e.UsuarioId == request.UsuarioId) &&
            (request.Accion == null || e.Accion == request.Accion) &&
            (request.Recurso == null || e.Recurso == request.Recurso) &&
            e.OcurridoAt >= desde && e.OcurridoAt <= hasta,
            ct)).ToList();

        var total = all.Count;
        var items = all.OrderByDescending(e => e.OcurridoAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(e => new BitacoraEventoDto
            {
                Id = e.Id,
                UsuarioId = e.UsuarioId,
                UsuarioEmail = e.UsuarioEmail,
                Accion = e.Accion.ToString(),
                Recurso = e.Recurso,
                RecursoId = e.RecursoId,
                Descripcion = e.Descripcion,
                DatosAntes = e.DatosAntes,
                DatosDespues = e.DatosDespues,
                IpOrigen = e.IpOrigen,
                Exitoso = e.Exitoso,
                OcurridoAt = e.OcurridoAt
            });

        return new PagedResult<BitacoraEventoDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }
}
