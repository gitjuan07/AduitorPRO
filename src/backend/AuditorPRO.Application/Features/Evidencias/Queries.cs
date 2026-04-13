using AuditorPRO.Application.Common.Models;
using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.Evidencias;

public record GetEvidenciasQuery(
    Guid? HallazgoId = null,
    Guid? SimulacionId = null,
    TipoEvidencia? Tipo = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<EvidenciaDto>>;

public class EvidenciaDto
{
    public Guid Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ContentType { get; set; }
    public long TamanoBytes { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string TipoEvidencia { get; set; } = string.Empty;
    public Guid? HallazgoId { get; set; }
    public Guid? SimulacionId { get; set; }
    public string? SubidoPor { get; set; }
    public DateTime SubidoAt { get; set; }
    public string? SasUrl { get; set; }
}

public class GetEvidenciasHandler : IRequestHandler<GetEvidenciasQuery, PagedResult<EvidenciaDto>>
{
    private readonly IRepository<Evidencia> _repo;
    private readonly IBlobStorageService _blob;

    public GetEvidenciasHandler(IRepository<Evidencia> repo, IBlobStorageService blob)
    { _repo = repo; _blob = blob; }

    public async Task<PagedResult<EvidenciaDto>> Handle(GetEvidenciasQuery request, CancellationToken ct)
    {
        var all = (await _repo.FindAsync(e =>
            (request.HallazgoId == null || e.HallazgoId == request.HallazgoId) &&
            (request.SimulacionId == null || e.SimulacionId == request.SimulacionId) &&
            (request.Tipo == null || e.TipoEvidencia == request.Tipo),
            ct)).ToList();

        var total = all.Count;
        var page = all.OrderByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ToList();

        var dtos = new List<EvidenciaDto>();
        foreach (var e in page)
        {
            string? sasUrl = null;
            try { sasUrl = await _blob.GenerateSasTokenAsync(e.BlobUrl, TimeSpan.FromHours(1), ct); }
            catch { /* Storage no configurado — devolver sin SAS */ }
            dtos.Add(new EvidenciaDto
            {
                Id = e.Id,
                NombreArchivo = e.NombreArchivo,
                Descripcion = e.DescripcionArchivo,
                ContentType = e.ContentType,
                TamanoBytes = e.TamanoBytes,
                BlobUrl = e.BlobUrl,
                TipoEvidencia = e.TipoEvidencia.ToString(),
                HallazgoId = e.HallazgoId,
                SimulacionId = e.SimulacionId,
                SubidoPor = e.SubidoPor,
                SubidoAt = e.CreatedAt,
                SasUrl = sasUrl
            });
        }

        return new PagedResult<EvidenciaDto> { Items = dtos, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }
}
