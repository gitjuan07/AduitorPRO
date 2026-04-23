namespace AuditorPRO.Domain.Interfaces;

public interface IEntraIdSyncService
{
    /// <summary>
    /// Sincroniza usuarios desde Microsoft Graph y crea un SnapshotEntraID con origen GRAPH_DIRECT.
    /// </summary>
    Task<EntraIdSyncResultado> SincronizarAsync(string? nombreInstantanea, CancellationToken ct = default);
}

public record EntraIdSyncResultado(
    Guid SnapshotId,
    string Nombre,
    DateTime FechaInstantanea,
    int TotalRegistros,
    int Errores,
    List<string> DetalleErrores,
    string Origen
);
