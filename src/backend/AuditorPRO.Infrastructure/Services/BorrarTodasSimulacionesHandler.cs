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

        await _db.Database.ExecuteSqlRawAsync("ALTER TABLE [Evidencias] NOCHECK CONSTRAINT ALL", ct);
        await _db.Database.ExecuteSqlRawAsync("ALTER TABLE [Hallazgos] NOCHECK CONSTRAINT ALL", ct);
        await _db.Database.ExecuteSqlRawAsync("ALTER TABLE [ResultadosControl] NOCHECK CONSTRAINT ALL", ct);
        await _db.Database.ExecuteSqlRawAsync("ALTER TABLE [FuentesDatosSimulacion] NOCHECK CONSTRAINT ALL", ct);

        try
        {
            await ExecDelete("Evidencias", ct);
            await ExecDelete("Hallazgos", ct);
            await ExecDelete("ResultadosControl", ct);
            await ExecDelete("FuentesDatosSimulacion", ct);
            await ExecDelete("Simulaciones", ct);
        }
        finally
        {
            await _db.Database.ExecuteSqlRawAsync("ALTER TABLE [Evidencias] CHECK CONSTRAINT ALL", ct);
            await _db.Database.ExecuteSqlRawAsync("ALTER TABLE [Hallazgos] CHECK CONSTRAINT ALL", ct);
            await _db.Database.ExecuteSqlRawAsync("ALTER TABLE [ResultadosControl] CHECK CONSTRAINT ALL", ct);
            await _db.Database.ExecuteSqlRawAsync("ALTER TABLE [FuentesDatosSimulacion] CHECK CONSTRAINT ALL", ct);
        }

        _logger.LogInformation("BorrarTodas: completado OK");
        return total;
    }

    private async Task ExecDelete(string tabla, CancellationToken ct)
    {
        try
        {
            var rows = await _db.Database.ExecuteSqlRawAsync($"DELETE FROM [{tabla}]", ct);
            _logger.LogInformation("DELETE [{Tabla}]: {Rows} filas", tabla, rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR DELETE [{Tabla}]: {Msg} | Inner: {Inner}",
                tabla, ex.Message, ex.InnerException?.Message);
            throw new Exception($"Fallo en DELETE [{tabla}]: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }
}
