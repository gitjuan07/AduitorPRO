using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.BaseConocimiento;

public record BaseConocimientoDto(
    Guid    Id,
    string  NombreArchivo,
    string  RutaOriginal,
    string  TipoArchivo,
    long    TamanoBytes,
    int     TotalPalabras,
    string? DominioDetectado,
    string? ControlesDetectados,
    string? Tags,
    string  Estado,
    string  FuenteIngesta,
    string? IngresadoPor,
    DateTime CreadoAt,
    string  Resumen
);

// ─── Listar base de conocimiento ──────────────────────────────────────────────
public record GetBaseConocimientoQuery(
    string? Dominio   = null,
    string? Busqueda  = null,
    int     Page      = 1,
    int     PageSize  = 20
) : IRequest<GetBaseConocimientoResult>;

public record GetBaseConocimientoResult(List<BaseConocimientoDto> Items, int Total);

public class GetBaseConocimientoHandler : IRequestHandler<GetBaseConocimientoQuery, GetBaseConocimientoResult>
{
    private readonly IIngestorDocumentosService _ingestor;
    private readonly IBaseConocimientoRepository _repo;

    public GetBaseConocimientoHandler(IIngestorDocumentosService ingestor, IBaseConocimientoRepository repo)
    {
        _ingestor = ingestor;
        _repo = repo;
    }

    public async Task<GetBaseConocimientoResult> Handle(GetBaseConocimientoQuery req, CancellationToken ct)
    {
        List<Domain.Entities.BaseConocimiento> items;

        if (!string.IsNullOrWhiteSpace(req.Busqueda))
            items = await _ingestor.BuscarAsync(req.Busqueda, 100, ct);
        else
            items = await _repo.ListarAsync(req.Dominio, ct);

        var total = items.Count;
        var paginados = items
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(b => new BaseConocimientoDto(
                b.Id, b.NombreArchivo, b.RutaOriginal, b.TipoArchivo,
                b.TamanoBytes, b.TotalPalabras,
                b.DominioDetectado, b.ControlesDetectados, b.Tags,
                b.Estado, b.FuenteIngesta, b.IngresadoPor, b.CreadoAt,
                b.TextoCompleto.Length > 500 ? b.TextoCompleto[..500] + "..." : b.TextoCompleto
            ))
            .ToList();

        return new GetBaseConocimientoResult(paginados, total);
    }
}

// ─── Buscar contexto relevante para IA (RAG) ──────────────────────────────────
public record BuscarContextoIAQuery(string Query, int TopK = 5) : IRequest<string>;

public class BuscarContextoIAHandler : IRequestHandler<BuscarContextoIAQuery, string>
{
    private readonly IIngestorDocumentosService _ingestor;
    public BuscarContextoIAHandler(IIngestorDocumentosService ingestor) => _ingestor = ingestor;

    public async Task<string> Handle(BuscarContextoIAQuery req, CancellationToken ct)
    {
        var docs = await _ingestor.BuscarAsync(req.Query, req.TopK, ct);
        if (!docs.Any()) return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== CONTEXTO DE BASE DE CONOCIMIENTO ===");
        foreach (var doc in docs)
        {
            sb.AppendLine($"\n--- Documento: {doc.NombreArchivo} | Dominio: {doc.DominioDetectado ?? "General"} ---");
            var fragmento = doc.TextoCompleto.Length > 1000
                ? doc.TextoCompleto[..1000] + "..."
                : doc.TextoCompleto;
            sb.AppendLine(fragmento);
        }
        sb.AppendLine("=== FIN CONTEXTO ===");
        return sb.ToString();
    }
}
