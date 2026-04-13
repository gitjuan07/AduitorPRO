using AuditorPRO.Application.Common.Models;
using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.Conectores;

public record GetConectoresQuery(int Page = 1, int PageSize = 50) : IRequest<PagedResult<ConectorDto>>;

public class ConectorDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Sistema { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoConexion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? UltimaEjecucion { get; set; }
    public bool UltimaEjecucionExito { get; set; }
    public string? UltimoError { get; set; }
    public int TotalEjecuciones { get; set; }
    public string? UrlEndpoint { get; set; }
    public string? AuthType { get; set; }
    public string? ConfiguracionJson { get; set; }
    public string? SecretKeyVaultRef { get; set; }
}

public class GetConectoresHandler : IRequestHandler<GetConectoresQuery, PagedResult<ConectorDto>>
{
    private readonly IRepository<Conector> _repo;
    public GetConectoresHandler(IRepository<Conector> repo) => _repo = repo;

    public async Task<PagedResult<ConectorDto>> Handle(GetConectoresQuery request, CancellationToken ct)
    {
        var all = (await _repo.GetAllAsync(ct)).ToList();
        var total = all.Count;
        var items = all.OrderBy(c => c.Nombre)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(c => new ConectorDto
            {
                Id = c.Id, Nombre = c.Nombre, Sistema = c.Sistema,
                Descripcion = c.Descripcion, TipoConexion = c.TipoConector.ToString(),
                Estado = c.Estado.ToString(), UltimaEjecucion = c.UltimaEjecucion,
                UltimaEjecucionExito = c.UltimaEjecucionExito, UltimoError = c.UltimoError,
                TotalEjecuciones = c.TotalEjecuciones, UrlEndpoint = c.UrlEndpoint, AuthType = c.AuthType,
                ConfiguracionJson = c.ConfiguracionJson, SecretKeyVaultRef = c.SecretKeyVaultRef
            });

        return new PagedResult<ConectorDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }
}

public record GetConectorLogsQuery(Guid ConectorId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<LogConectorDto>>;

public class LogConectorDto
{
    public Guid Id { get; set; }
    public bool Exitoso { get; set; }
    public int? RegistrosProcesados { get; set; }
    public string? MensajeError { get; set; }
    public int DuracionMs { get; set; }
    public DateTime EjecutadoAt { get; set; }
    public string? EjecutadoPor { get; set; }
}

public class GetConectorLogsHandler : IRequestHandler<GetConectorLogsQuery, PagedResult<LogConectorDto>>
{
    private readonly IRepository<LogConector> _repo;
    public GetConectorLogsHandler(IRepository<LogConector> repo) => _repo = repo;

    public async Task<PagedResult<LogConectorDto>> Handle(GetConectorLogsQuery request, CancellationToken ct)
    {
        var all = (await _repo.FindAsync(l => l.ConectorId == request.ConectorId, ct)).ToList();
        var total = all.Count;
        var items = all.OrderByDescending(l => l.EjecutadoAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(l => new LogConectorDto
            {
                Id = l.Id, Exitoso = l.Exitoso, RegistrosProcesados = l.RegistrosProcesados,
                MensajeError = l.MensajeError, DuracionMs = l.DuracionMs,
                EjecutadoAt = l.EjecutadoAt, EjecutadoPor = l.EjecutadoPor
            });

        return new PagedResult<LogConectorDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }
}
