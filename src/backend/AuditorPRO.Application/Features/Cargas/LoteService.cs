using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.Cargas;

// ── Query: últimos lotes vigentes ────────────────────────────────────────────
public record GetLotesCargaQuery(string? TipoCarga = null, string? SociedadCodigo = null, int Limit = 50)
    : IRequest<List<LoteCargaDto>>;

public record LoteCargaDto(
    Guid Id,
    string TipoCarga,
    DateTime FechaCarga,
    string? SociedadCodigo,
    string? SociedadNombre,
    string? NombreArchivo,
    int TotalRegistros,
    int Insertados,
    int Actualizados,
    int Errores,
    string? CargadoPor,
    bool EsVigente
);

public class GetLotesCargaHandler : IRequestHandler<GetLotesCargaQuery, List<LoteCargaDto>>
{
    private readonly IRepository<LoteCarga> _repo;
    public GetLotesCargaHandler(IRepository<LoteCarga> repo) => _repo = repo;

    public async Task<List<LoteCargaDto>> Handle(GetLotesCargaQuery req, CancellationToken ct)
    {
        var todos = await _repo.GetAllAsync(ct);
        var query = todos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.TipoCarga))
            query = query.Where(l => l.TipoCarga == req.TipoCarga);

        if (!string.IsNullOrWhiteSpace(req.SociedadCodigo))
            query = query.Where(l => l.SociedadCodigo == req.SociedadCodigo);

        return query
            .OrderByDescending(l => l.FechaCarga)
            .Take(req.Limit)
            .Select(l => new LoteCargaDto(
                l.Id, l.TipoCarga, l.FechaCarga, l.SociedadCodigo, l.SociedadNombre,
                l.NombreArchivo, l.TotalRegistros, l.Insertados, l.Actualizados, l.Errores,
                l.CargadoPor, l.EsVigente))
            .ToList();
    }
}

// ── Query: último lote vigente por tipo+sociedad (para simulaciones) ─────────
public record GetUltimoLoteQuery(string TipoCarga, string? SociedadCodigo = null)
    : IRequest<LoteCargaDto?>;

public class GetUltimoLoteHandler : IRequestHandler<GetUltimoLoteQuery, LoteCargaDto?>
{
    private readonly IRepository<LoteCarga> _repo;
    public GetUltimoLoteHandler(IRepository<LoteCarga> repo) => _repo = repo;

    public async Task<LoteCargaDto?> Handle(GetUltimoLoteQuery req, CancellationToken ct)
    {
        var todos = await _repo.GetAllAsync(ct);
        var l = todos
            .Where(x => x.TipoCarga == req.TipoCarga && x.EsVigente)
            .Where(x => req.SociedadCodigo == null || x.SociedadCodigo == req.SociedadCodigo)
            .OrderByDescending(x => x.FechaCarga)
            .FirstOrDefault();

        if (l is null) return null;
        return new LoteCargaDto(l.Id, l.TipoCarga, l.FechaCarga, l.SociedadCodigo, l.SociedadNombre,
            l.NombreArchivo, l.TotalRegistros, l.Insertados, l.Actualizados, l.Errores, l.CargadoPor, l.EsVigente);
    }
}

// ── Helper: crear lote y marcar anteriores como no vigentes ─────────────────
public static class LoteHelper
{
    public static async Task<LoteCarga> CrearLoteAsync(
        IRepository<LoteCarga> repo,
        string tipoCarga,
        string? sociedadCodigo,
        string? sociedadNombre,
        string? nombreArchivo,
        CargaResultado resultado,
        string? cargadoPor,
        CancellationToken ct)
    {
        // Marcar anteriores del mismo tipo+sociedad como no vigentes
        var anteriores = (await repo.GetAllAsync(ct))
            .Where(l => l.TipoCarga == tipoCarga
                     && l.EsVigente
                     && (sociedadCodigo == null || l.SociedadCodigo == sociedadCodigo))
            .ToList();

        foreach (var ant in anteriores)
            ant.EsVigente = false;

        var lote = new LoteCarga
        {
            TipoCarga      = tipoCarga,
            FechaCarga     = DateTime.UtcNow,
            SociedadCodigo = sociedadCodigo,
            SociedadNombre = sociedadNombre,
            NombreArchivo  = nombreArchivo,
            TotalRegistros = resultado.TotalRegistros,
            Insertados     = resultado.Insertados,
            Actualizados   = resultado.Actualizados,
            Errores        = resultado.Errores,
            CargadoPor     = cargadoPor,
            EsVigente      = true,
        };

        await repo.AddAsync(lote, ct);
        await repo.SaveChangesAsync(ct);
        return lote;
    }
}

// ── Command: purgar cargas antiguas — conserva solo el lote más reciente por tipo ──
public record PurgarCargasAntiguasCommand : IRequest<PurgarCargasResultado>;

public record PurgarCargasResultado(
    int LotesBorrados,
    int RegistrosBorrados,
    Dictionary<string, int> DetallesPorTipo
);
