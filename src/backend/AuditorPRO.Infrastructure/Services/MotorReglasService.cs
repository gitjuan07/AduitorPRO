using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AuditorPRO.Infrastructure.Persistence;

namespace AuditorPRO.Infrastructure.Services;

public class MotorReglasService : IMotorReglasService
{
    private readonly AppDbContext _db;
    private readonly IAzureOpenAIService _ia;
    private readonly ILogger<MotorReglasService> _logger;

    public MotorReglasService(AppDbContext db, IAzureOpenAIService ia, ILogger<MotorReglasService> logger)
    {
        _db = db;
        _ia = ia;
        _logger = logger;
    }

    public async Task<ResultadoEjecucion> EjecutarSimulacionAsync(SimulacionAuditoria simulacion, CancellationToken ct = default)
    {
        var sociedadIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(simulacion.SociedadIds ?? "[]") ?? [];
        var dominioIds = simulacion.DominioIds != null
            ? System.Text.Json.JsonSerializer.Deserialize<List<int>>(simulacion.DominioIds) ?? []
            : null;

        var query = _db.PuntosControl.Include(p => p.Dominio).Where(p => p.Activo);
        if (dominioIds?.Count > 0)
            query = query.Where(p => dominioIds.Contains(p.DominioId));

        var controles = await query.ToListAsync(ct);
        var resultados = new List<ResultadoControl>();
        var hallazgos = new List<Hallazgo>();

        foreach (var control in controles)
        {
            foreach (var sociedadId in sociedadIds.Any() ? sociedadIds : new List<int> { 0 })
            {
                var resultado = await EvaluarControlAsync(control, simulacion, sociedadId == 0 ? null : sociedadId, ct);
                resultados.Add(resultado);

                if (resultado.Semaforo != SemaforoColor.VERDE)
                {
                    var hallazgo = GenerarHallazgo(simulacion, resultado, control);
                    hallazgos.Add(hallazgo);
                }
            }
        }

        var score = CalcularScoreMadurez(resultados, hallazgos);

        return new ResultadoEjecucion
        {
            TotalControles = resultados.Count,
            ControlesVerde = resultados.Count(r => r.Semaforo == SemaforoColor.VERDE),
            ControlesAmarillo = resultados.Count(r => r.Semaforo == SemaforoColor.AMARILLO),
            ControlesRojo = resultados.Count(r => r.Semaforo == SemaforoColor.ROJO),
            ScoreMadurez = score.Score,
            PorcentajeCumplimiento = score.PorcentajeCumplimiento,
            Resultados = resultados,
            HallazgosGenerados = hallazgos
        };
    }

    private async Task<ResultadoControl> EvaluarControlAsync(
        PuntoControl control, SimulacionAuditoria simulacion, int? sociedadId, CancellationToken ct)
    {
        SemaforoColor semaforo;
        string resultadoDetalle;

        try
        {
            semaforo = await EvaluarReglaNegocioAsync(control, sociedadId, simulacion, ct);
            resultadoDetalle = GenerarDescripcionResultado(control, semaforo, sociedadId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluando control {Codigo}", control.Codigo);
            semaforo = SemaforoColor.NO_EVALUADO;
            resultadoDetalle = $"No se pudo evaluar automáticamente: {ex.Message}";
        }

        string? analisisIa = null;
        string? recomendacion = null;

        if (semaforo == SemaforoColor.ROJO && control.TipoEvaluacion != TipoEvaluacion.MANUAL)
        {
            try
            {
                analisisIa = await _ia.AnalizarControlAsync(
                    $"Sistema: {control.Dominio?.Nombre}, Norma: {control.NormaReferencia}",
                    control.Nombre,
                    resultadoDetalle,
                    ct);
                recomendacion = await _ia.GenerarRecomendacionAsync(resultadoDetalle, control.Dominio?.Nombre ?? "", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error en análisis IA para control {Codigo}", control.Codigo);
            }
        }

        return new ResultadoControl
        {
            SimulacionId = simulacion.Id,
            PuntoControlId = control.Id,
            SociedadId = sociedadId,
            Semaforo = semaforo,
            Criticidad = control.CriticidadBase,
            ResultadoDetalle = resultadoDetalle,
            AnalisisIa = analisisIa,
            Recomendacion = recomendacion,
            EvaluadoAt = DateTime.UtcNow
        };
    }

    private async Task<SemaforoColor> EvaluarReglaNegocioAsync(
        PuntoControl control, int? sociedadId, SimulacionAuditoria simulacion, CancellationToken ct)
    {
        // Evaluación automática según código de control
        return control.Codigo switch
        {
            "ID-001" => await EvaluarUsuariosSinEmpleadoAsync(sociedadId, ct),
            "ID-002" => await EvaluarEmpleadosBajaConAccesoAsync(sociedadId, ct),
            "ID-003" => await EvaluarUsuariosSin90DiasAsync(sociedadId, ct),
            "RECERT-001" => await EvaluarRecertificacionAsync(sociedadId, ct),
            "SAP-001" => await EvaluarRolesCriticosSAPAsync(sociedadId, ct),
            "SOD-001" => await EvaluarConflictosSoDAsync(sociedadId, ct),
            _ when control.TipoEvaluacion == TipoEvaluacion.MANUAL => SemaforoColor.NO_EVALUADO,
            _ => EvaluarPorCondiciones(control)
        };
    }

    private async Task<SemaforoColor> EvaluarUsuariosSinEmpleadoAsync(int? sociedadId, CancellationToken ct)
    {
        var query = _db.UsuariosSistema
            .Where(u => u.Estado == EstadoUsuario.ACTIVO && u.EmpleadoId == null && !u.IsDeleted);
        var count = await query.CountAsync(ct);
        return count == 0 ? SemaforoColor.VERDE : count <= 5 ? SemaforoColor.AMARILLO : SemaforoColor.ROJO;
    }

    private async Task<SemaforoColor> EvaluarEmpleadosBajaConAccesoAsync(int? sociedadId, CancellationToken ct)
    {
        var empleadosInactivos = await _db.Empleados
            .Where(e => e.EstadoLaboral == EstadoLaboral.INACTIVO || e.EstadoLaboral == EstadoLaboral.BAJA_PROCESADA)
            .Select(e => e.Id)
            .ToListAsync(ct);

        if (!empleadosInactivos.Any()) return SemaforoColor.VERDE;

        var count = await _db.UsuariosSistema
            .Where(u => u.EmpleadoId.HasValue && empleadosInactivos.Contains(u.EmpleadoId.Value)
                     && u.Estado == EstadoUsuario.ACTIVO)
            .CountAsync(ct);

        return count == 0 ? SemaforoColor.VERDE : count <= 2 ? SemaforoColor.AMARILLO : SemaforoColor.ROJO;
    }

    private async Task<SemaforoColor> EvaluarUsuariosSin90DiasAsync(int? sociedadId, CancellationToken ct)
    {
        var corte = DateTime.UtcNow.AddDays(-90);
        var count = await _db.UsuariosSistema
            .Where(u => u.Estado == EstadoUsuario.ACTIVO
                     && (u.FechaUltimoAcceso == null || u.FechaUltimoAcceso < corte))
            .CountAsync(ct);
        return count == 0 ? SemaforoColor.VERDE : count <= 10 ? SemaforoColor.AMARILLO : SemaforoColor.ROJO;
    }

    private Task<SemaforoColor> EvaluarRecertificacionAsync(int? sociedadId, CancellationToken ct)
        => Task.FromResult(SemaforoColor.NO_EVALUADO);

    private async Task<SemaforoColor> EvaluarRolesCriticosSAPAsync(int? sociedadId, CancellationToken ct)
    {
        var rolesCriticos = await _db.Set<RolSistema>()
            .Where(r => r.Sistema == "SAP" && r.EsCritico && r.Activo)
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (!rolesCriticos.Any()) return SemaforoColor.VERDE;

        var asignacionesSinExpediente = await _db.Set<AsignacionRolUsuario>()
            .Where(a => rolesCriticos.Contains(a.RolId) && a.Activa && a.ExpedienteRef == null)
            .CountAsync(ct);

        return asignacionesSinExpediente == 0 ? SemaforoColor.VERDE : SemaforoColor.ROJO;
    }

    private async Task<SemaforoColor> EvaluarConflictosSoDAsync(int? sociedadId, CancellationToken ct)
    {
        var conflictos = await _db.Set<ConflictoSoD>()
            .Where(c => c.Activo && (c.Riesgo == "CRITICO" || c.Riesgo == "ALTO"))
            .CountAsync(ct);
        return conflictos == 0 ? SemaforoColor.VERDE : conflictos <= 2 ? SemaforoColor.AMARILLO : SemaforoColor.ROJO;
    }

    private static SemaforoColor EvaluarPorCondiciones(PuntoControl control)
    {
        // Para controles sin lógica específica, devolver NO_EVALUADO si es MANUAL
        return control.TipoEvaluacion == TipoEvaluacion.SEMI_AUTOMATICO
            ? SemaforoColor.AMARILLO
            : SemaforoColor.NO_EVALUADO;
    }

    private static string GenerarDescripcionResultado(PuntoControl control, SemaforoColor semaforo, int? sociedadId)
    {
        return semaforo switch
        {
            SemaforoColor.VERDE => $"Control {control.Codigo} cumple con los criterios establecidos.",
            SemaforoColor.AMARILLO => $"Control {control.Codigo} presenta observaciones menores que requieren atención.",
            SemaforoColor.ROJO => $"Control {control.Codigo} NO cumple. Se requiere acción correctiva inmediata.",
            _ => $"Control {control.Codigo} no pudo ser evaluado automáticamente. Requiere revisión manual."
        };
    }

    private static Hallazgo GenerarHallazgo(SimulacionAuditoria simulacion, ResultadoControl resultado, PuntoControl control)
    {
        return new Hallazgo
        {
            SimulacionId = simulacion.Id,
            ResultadoControlId = resultado.Id,
            SociedadId = resultado.SociedadId,
            Titulo = $"[{control.Codigo}] {control.Nombre}",
            Descripcion = resultado.ResultadoDetalle ?? control.Descripcion ?? control.Nombre,
            Criticidad = control.CriticidadBase,
            Estado = EstadoHallazgo.ABIERTO,
            NormaAfectada = control.NormaReferencia,
            AnalisisIa = resultado.AnalisisIa,
            CreatedBy = simulacion.IniciadaPor
        };
    }

    private static (decimal Score, decimal PorcentajeCumplimiento) CalcularScoreMadurez(
        List<ResultadoControl> resultados, List<Hallazgo> hallazgos)
    {
        if (!resultados.Any()) return (0, 0);

        var total = resultados.Count;
        var verdes = resultados.Count(r => r.Semaforo == SemaforoColor.VERDE);
        var amarillos = resultados.Count(r => r.Semaforo == SemaforoColor.AMARILLO);

        var pctVerdes = (double)verdes / total;
        var pctAmarillos = (double)amarillos / total;
        var hallazgosCriticosRepetidos = hallazgos.Count(h => h.Criticidad == Criticidad.CRITICA);

        // Algoritmo del blueprint
        var score = (pctVerdes * 4.0) + (pctAmarillos * 2.0);

        // Cobertura de evidencia (asumida 0.6 sin datos reales)
        score += 0.6 * 2.0;

        // Penalización por hallazgos críticos repetidos
        score -= Math.Min(hallazgosCriticosRepetidos * 0.1, 0.5);

        // Normalizar a escala 1-10
        score = Math.Max(1.0, Math.Min(10.0, score));

        var porcentaje = (decimal)(pctVerdes * 100);

        return ((decimal)Math.Round(score, 1), Math.Round(porcentaje, 1));
    }
}
