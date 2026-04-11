using AuditorPRO.Domain.Entities;

namespace AuditorPRO.Domain.Interfaces;

public record IngestResultado(int Procesados, int Errores, int Omitidos, List<string> Detalles);

public interface IIngestorDocumentosService
{
    Task<IngestResultado> IngestirDirectorioAsync(string rutaDirectorio, string? usuario, CancellationToken ct = default);
    Task<IngestResultado> IngestirArchivoAsync(string rutaArchivo, string? usuario, CancellationToken ct = default);
    Task<IngestResultado> IngestirStreamAsync(Stream stream, string nombreArchivo, string? usuario, CancellationToken ct = default);
    Task<List<BaseConocimiento>> BuscarAsync(string query, int topK = 5, CancellationToken ct = default);
}
