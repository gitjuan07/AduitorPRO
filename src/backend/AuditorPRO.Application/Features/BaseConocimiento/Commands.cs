using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.BaseConocimiento;

// ─── Ingestir directorio ───────────────────────────────────────────────────────
public record IngestirDirectorioCommand(string RutaDirectorio, string? Usuario)
    : IRequest<IngestResultado>;

public class IngestirDirectorioHandler : IRequestHandler<IngestirDirectorioCommand, IngestResultado>
{
    private readonly IIngestorDocumentosService _ingestor;
    public IngestirDirectorioHandler(IIngestorDocumentosService ingestor) => _ingestor = ingestor;

    public Task<IngestResultado> Handle(IngestirDirectorioCommand req, CancellationToken ct)
        => _ingestor.IngestirDirectorioAsync(req.RutaDirectorio, req.Usuario, ct);
}

// ─── Ingestir archivo único (upload desde browser) ────────────────────────────
public record IngestirArchivoUploadCommand(Stream Stream, string NombreArchivo, string? Usuario)
    : IRequest<IngestResultado>;

public class IngestirArchivoUploadHandler : IRequestHandler<IngestirArchivoUploadCommand, IngestResultado>
{
    private readonly IIngestorDocumentosService _ingestor;
    public IngestirArchivoUploadHandler(IIngestorDocumentosService ingestor) => _ingestor = ingestor;

    public Task<IngestResultado> Handle(IngestirArchivoUploadCommand req, CancellationToken ct)
        => _ingestor.IngestirStreamAsync(req.Stream, req.NombreArchivo, req.Usuario, ct);
}

// ─── Eliminar documento de la base de conocimiento ───────────────────────────
public record EliminarBaseConocimientoCommand(Guid Id) : IRequest<bool>;

public class EliminarBaseConocimientoHandler : IRequestHandler<EliminarBaseConocimientoCommand, bool>
{
    private readonly IRepository<Domain.Entities.BaseConocimiento> _repo;
    public EliminarBaseConocimientoHandler(IRepository<Domain.Entities.BaseConocimiento> repo) => _repo = repo;

    public async Task<bool> Handle(EliminarBaseConocimientoCommand req, CancellationToken ct)
    {
        var doc = await _repo.GetByIdAsync(req.Id, ct);
        if (doc == null) return false;
        doc.IsDeleted = true;
        await _repo.UpdateAsync(doc, ct);
        await _repo.SaveChangesAsync(ct);
        return true;
    }
}
