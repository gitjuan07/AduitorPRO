using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.Cargas;

// ─── Sincronización directa Entra ID via Microsoft Graph ────────────────────

public record SyncEntraIDDirectoCommand(
    string? NombreInstantanea = null
) : IRequest<SyncEntraIDDirectoResultado>;

public record SyncEntraIDDirectoResultado(
    Guid SnapshotId,
    string Nombre,
    DateTime FechaInstantanea,
    int TotalRegistros,
    int Errores,
    List<string> DetalleErrores,
    string Origen
);

public class SyncEntraIDDirectoHandler : IRequestHandler<SyncEntraIDDirectoCommand, SyncEntraIDDirectoResultado>
{
    private readonly IEntraIdSyncService _syncService;
    private readonly IAuditLoggerService _audit;
    private readonly ICurrentUserService _user;

    public SyncEntraIDDirectoHandler(
        IEntraIdSyncService syncService,
        IAuditLoggerService audit,
        ICurrentUserService user)
    {
        _syncService = syncService;
        _audit = audit;
        _user = user;
    }

    public async Task<SyncEntraIDDirectoResultado> Handle(
        SyncEntraIDDirectoCommand request, CancellationToken ct)
    {
        var result = await _syncService.SincronizarAsync(request.NombreInstantanea, ct);

        if (result.SnapshotId != Guid.Empty)
        {
            await _audit.LogAsync(
                _user.UserId, _user.Email,
                "SYNC_ENTRAID_GRAPH", "SnapshotEntraID",
                result.SnapshotId.ToString(),
                datosDespues: new { result.TotalRegistros, result.Errores, result.Origen },
                ct: ct);
        }

        return new SyncEntraIDDirectoResultado(
            result.SnapshotId,
            result.Nombre,
            result.FechaInstantanea,
            result.TotalRegistros,
            result.Errores,
            result.DetalleErrores,
            result.Origen);
    }
}
