using AuditorPRO.Application.Features.Simulaciones;
using AuditorPRO.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuditorPRO.Infrastructure.Services;

public class BorrarTodasSimulacionesHandler : IRequestHandler<BorrarTodasSimulacionesCommand, int>
{
    private readonly AppDbContext _db;
    private readonly ILogger<BorrarTodasSimulacionesHandler> _logger;

    public BorrarTodasSimulacionesHandler(AppDbContext db, ILogger<BorrarTodasSimulacionesHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> Handle(BorrarTodasSimulacionesCommand _, CancellationToken ct)
    {
        var total = await _db.Simulaciones.IgnoreQueryFilters().CountAsync(ct);
        _logger.LogInformation("BorrarTodas: {Total} simulaciones a borrar", total);
        if (total == 0) return 0;

        // Orden respeta las FK: Evidencias → Hallazgos → ResultadosControl → FuentesDatosSimulacion → Simulaciones
        await DeleteStep("Evidencias", ct);
        await DeleteStep("Hallazgos", ct);
        await DeleteStep("ResultadosControl", ct);
        await DeleteStep("FuentesDatosSimulacion", ct);
        await DeleteStep("Simulaciones", ct);

        _logger.LogInformation("BorrarTodas: completado OK");
        return total;
    }

    private async Task DeleteStep(string tabla, CancellationToken ct)
    {
        try
        {
            var rows = await _db.Database.ExecuteSqlRawAsync($"DELETE FROM [{tabla}]", ct);
            _logger.LogInformation("DELETE [{Tabla}]: {Rows} filas", tabla, rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR al DELETE [{Tabla}]: {Msg} | Inner: {Inner}",
                tabla, ex.Message, ex.InnerException?.Message);
            throw new Exception($"Fallo en DELETE [{tabla}]: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }
}
