using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using AuditorPRO.Infrastructure.Helpers;
using AuditorPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        _logger.LogInformation("Iniciando simulación {Id}: {N} controles, sociedades={Socs}",
            simulacion.Id, controles.Count, string.Join(",", sociedadIds));

        foreach (var control in controles)
        {
            foreach (var sociedadId in sociedadIds.Any() ? sociedadIds : new List<int> { 0 })
            {
                var resultado = await EvaluarControlAsync(control, simulacion, sociedadId == 0 ? null : sociedadId, ct);
                resultados.Add(resultado);

                if (resultado.Semaforo != SemaforoColor.VERDE && resultado.Semaforo != SemaforoColor.NO_EVALUADO)
                {
                    var hallazgo = GenerarHallazgo(simulacion, resultado, control);
                    hallazgos.Add(hallazgo);
                }
            }
        }

        _logger.LogInformation("Simulación {Id} completada: {V} verde, {A} amarillo, {R} rojo, {H} hallazgos",
            simulacion.Id,
            resultados.Count(r => r.Semaforo == SemaforoColor.VERDE),
            resultados.Count(r => r.Semaforo == SemaforoColor.AMARILLO),
            resultados.Count(r => r.Semaforo == SemaforoColor.ROJO),
            hallazgos.Count);

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

    // ─── Evaluación de un control ─────────────────────────────────────────────

    private async Task<ResultadoControl> EvaluarControlAsync(
        PuntoControl control, SimulacionAuditoria simulacion, int? sociedadId, CancellationToken ct)
    {
        SemaforoColor semaforo;
        string resultadoDetalle;

        try
        {
            (semaforo, resultadoDetalle) = await EvaluarReglaNegocioAsync(control, sociedadId, ct);
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

    // ─── Despacho de reglas por código ────────────────────────────────────────

    private async Task<(SemaforoColor, string)> EvaluarReglaNegocioAsync(
        PuntoControl control, int? sociedadId, CancellationToken ct)
    {
        var (sem, detalle) = control.Codigo switch
        {
            // Controles clásicos (compatibilidad hacia atrás)
            "ID-001" => await EvaluarCC001Async(sociedadId, ct),
            "ID-002" => await EvaluarCC002Async(sociedadId, ct),
            "ID-003" => await EvaluarCC003Async(sociedadId, ct),
            "RECERT-001" => (SemaforoColor.NO_EVALUADO, "Requiere revisión manual de recertificación."),
            "SAP-001" => await EvaluarCC011Async(sociedadId, ct),
            "SOD-001" => await EvaluarCC012Async(sociedadId, ct),

            // Controles cruzados nuevos CC-001 a CC-012
            "CC-001" => await EvaluarCC001Async(sociedadId, ct),
            "CC-002" => await EvaluarCC002Async(sociedadId, ct),
            "CC-003" => await EvaluarCC003Async(sociedadId, ct),
            "CC-004" => await EvaluarCC004Async(ct),
            "CC-005" => await EvaluarCC005Async(ct),
            "CC-006" => await EvaluarCC006Async(ct),
            "CC-007" => await EvaluarCC007Async(ct),
            "CC-008" => await EvaluarCC008Async(ct),
            "CC-009" => await EvaluarCC009Async(ct),
            "CC-010" => await EvaluarCC010Async(ct),
            "CC-011" => await EvaluarCC011Async(sociedadId, ct),
            "CC-012" => await EvaluarCC012Async(sociedadId, ct),

            _ when control.TipoEvaluacion == TipoEvaluacion.MANUAL =>
                (SemaforoColor.NO_EVALUADO, $"Control {control.Codigo} requiere revisión manual."),
            _ => (control.TipoEvaluacion == TipoEvaluacion.SEMI_AUTOMATICO
                    ? SemaforoColor.AMARILLO
                    : SemaforoColor.NO_EVALUADO,
                  $"Control {control.Codigo}: evaluación semi-automática o sin regla específica.")
        };

        return (sem, detalle);
    }

    // ─── CC-001: Usuario SAP activo sin empleado maestro por cédula ───────────

    private async Task<(SemaforoColor, string)> EvaluarCC001Async(int? sociedadId, CancellationToken ct)
    {
        var sapActivos = await _db.UsuariosSistema
            .AsNoTracking()
            .Where(u => u.Sistema == "SAP" && u.Estado == EstadoUsuario.ACTIVO && !u.IsDeleted)
            .Select(u => new { u.NombreUsuario, u.Cedula, u.CedulaNormalizada, u.EmpleadoId })
            .ToListAsync(ct);

        var cedulasEmpleados = await _db.Empleados
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Select(e => new { e.Id, e.CedulaNormalizada, e.Cedula })
            .ToListAsync(ct);

        var idsEmpleados = cedulasEmpleados.Select(e => e.Id).ToHashSet();
        var cedulasNorm = cedulasEmpleados
            .Where(e => !string.IsNullOrWhiteSpace(e.CedulaNormalizada ?? e.Cedula))
            .Select(e => e.CedulaNormalizada ?? IdentityNormalizationHelper.NormalizarCedula(e.Cedula))
            .Where(c => c != null)
            .ToHashSet();

        var sinEmpleado = sapActivos.Where(u =>
        {
            // Primero intentar por EmpleadoId FK (enlace directo)
            if (u.EmpleadoId.HasValue && idsEmpleados.Contains(u.EmpleadoId.Value)) return false;
            // Luego por cédula normalizada
            var cedNorm = u.CedulaNormalizada ?? IdentityNormalizationHelper.NormalizarCedula(u.Cedula);
            return string.IsNullOrWhiteSpace(cedNorm) || !cedulasNorm.Contains(cedNorm);
        }).Count();

        _logger.LogInformation("CC-001: {N} usuarios SAP activos sin empleado maestro", sinEmpleado);

        return sinEmpleado == 0
            ? (SemaforoColor.VERDE, "Todos los usuarios SAP activos tienen empleado maestro asociado por cédula.")
            : sinEmpleado <= 5
                ? (SemaforoColor.AMARILLO, $"{sinEmpleado} usuario(s) SAP activo(s) sin correspondencia en nómina. Verificar si son cuentas de servicio o empleados aún no registrados.")
                : (SemaforoColor.ROJO, $"{sinEmpleado} usuario(s) SAP activos sin empleado maestro. Riesgo de accesos no autorizados o cuentas huérfanas.");
    }

    // ─── CC-002: Empleado inactivo/baja con usuario SAP activo ────────────────

    private async Task<(SemaforoColor, string)> EvaluarCC002Async(int? sociedadId, CancellationToken ct)
    {
        var empleadosBaja = await _db.Empleados
            .AsNoTracking()
            .Where(e => !e.IsDeleted &&
                (e.EstadoLaboral == EstadoLaboral.INACTIVO || e.EstadoLaboral == EstadoLaboral.BAJA_PROCESADA))
            .Select(e => new { e.Id, e.CedulaNormalizada, e.Cedula })
            .ToListAsync(ct);

        if (!empleadosBaja.Any())
            return (SemaforoColor.VERDE, "No hay empleados dados de baja con acceso SAP activo.");

        var cedulasBaja = empleadosBaja
            .Select(e => e.CedulaNormalizada ?? IdentityNormalizationHelper.NormalizarCedula(e.Cedula))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToHashSet();

        var idsBaja = empleadosBaja.Select(e => e.Id).ToHashSet();

        var sapActivos = await _db.UsuariosSistema
            .AsNoTracking()
            .Where(u => u.Sistema == "SAP" && u.Estado == EstadoUsuario.ACTIVO && !u.IsDeleted)
            .Select(u => new { u.NombreUsuario, u.Cedula, u.CedulaNormalizada, u.EmpleadoId })
            .ToListAsync(ct);

        var count = sapActivos.Count(u =>
        {
            if (u.EmpleadoId.HasValue && idsBaja.Contains(u.EmpleadoId.Value)) return true;
            var cedNorm = u.CedulaNormalizada ?? IdentityNormalizationHelper.NormalizarCedula(u.Cedula);
            return !string.IsNullOrWhiteSpace(cedNorm) && cedulasBaja.Contains(cedNorm);
        });

        _logger.LogInformation("CC-002: {N} empleados de baja con acceso SAP activo", count);

        return count == 0
            ? (SemaforoColor.VERDE, "Ningún empleado dado de baja conserva acceso SAP activo.")
            : count <= 2
                ? (SemaforoColor.AMARILLO, $"{count} empleado(s) de baja aún con acceso SAP activo. Revisar y deshabilitar accesos.")
                : (SemaforoColor.ROJO, $"CRÍTICO: {count} empleado(s) de baja o inactivos con acceso SAP activo. Acción inmediata requerida.");
    }

    // ─── CC-003: Usuario SAP sin ingreso en los últimos 90 días ──────────────

    private async Task<(SemaforoColor, string)> EvaluarCC003Async(int? sociedadId, CancellationToken ct)
    {
        var corte = DateTime.UtcNow.AddDays(-90);
        var count = await _db.UsuariosSistema
            .AsNoTracking()
            .Where(u => u.Sistema == "SAP" && u.Estado == EstadoUsuario.ACTIVO && !u.IsDeleted
                     && (u.FechaUltimoAcceso == null || u.FechaUltimoAcceso < corte))
            .CountAsync(ct);

        _logger.LogInformation("CC-003: {N} usuarios SAP sin acceso en 90+ días", count);

        return count == 0
            ? (SemaforoColor.VERDE, "Todos los usuarios SAP activos tuvieron acceso en los últimos 90 días.")
            : count <= 10
                ? (SemaforoColor.AMARILLO, $"{count} usuario(s) SAP activos sin acceso en los últimos 90 días. Considerar bloqueo o recertificación.")
                : (SemaforoColor.ROJO, $"{count} usuarios SAP activos sin acceso en 90+ días. Revisar política de cuentas inactivas.");
    }

    // ─── CC-004: Usuario Entra ID sin employeeId ──────────────────────────────

    private async Task<(SemaforoColor, string)> EvaluarCC004Async(CancellationToken ct)
    {
        var ultimoSnapshot = await _db.SnapshotsEntraID
            .AsNoTracking()
            .OrderByDescending(s => s.FechaInstantanea)
            .FirstOrDefaultAsync(ct);

        if (ultimoSnapshot == null)
            return (SemaforoColor.NO_EVALUADO, "No hay snapshot de Entra ID cargado. Sincronizar primero.");

        var count = await _db.RegistrosEntraID
            .AsNoTracking()
            .Where(r => r.SnapshotId == ultimoSnapshot.Id
                     && r.AccountEnabled
                     && string.IsNullOrWhiteSpace(r.EmployeeId))
            .CountAsync(ct);

        _logger.LogInformation("CC-004: {N} cuentas Entra ID activas sin EmployeeId (snapshot {Id})", count, ultimoSnapshot.Id);

        return count == 0
            ? (SemaforoColor.VERDE, "Todas las cuentas activas de Entra ID tienen EmployeeId asignado.")
            : count <= 3
                ? (SemaforoColor.AMARILLO, $"{count} cuenta(s) Entra ID activa(s) sin EmployeeId. Actualizar perfil de usuario en Azure AD.")
                : (SemaforoColor.ROJO, $"{count} cuentas Entra ID activas sin EmployeeId. Imposible vincular con nómina o SAP.");
    }

    // ─── CC-005: EmployeeId duplicado en Entra ID ─────────────────────────────

    private async Task<(SemaforoColor, string)> EvaluarCC005Async(CancellationToken ct)
    {
        var ultimoSnapshot = await _db.SnapshotsEntraID
            .AsNoTracking()
            .OrderByDescending(s => s.FechaInstantanea)
            .FirstOrDefaultAsync(ct);

        if (ultimoSnapshot == null)
            return (SemaforoColor.NO_EVALUADO, "No hay snapshot de Entra ID cargado.");

        var duplicados = await _db.RegistrosEntraID
            .AsNoTracking()
            .Where(r => r.SnapshotId == ultimoSnapshot.Id && !string.IsNullOrWhiteSpace(r.EmployeeId))
            .GroupBy(r => r.EmployeeId!)
            .Where(g => g.Count() > 1)
            .CountAsync(ct);

        _logger.LogInformation("CC-005: {N} EmployeeId duplicados en Entra ID", duplicados);

        return duplicados == 0
            ? (SemaforoColor.VERDE, "No existen EmployeeId duplicados en Entra ID.")
            : (SemaforoColor.ROJO, $"CRÍTICO: {duplicados} EmployeeId(s) duplicados en Entra ID. Indica posible error de datos o suplantación de identidad.");
    }

    // ─── CC-006: Cédula SAP vs employeeId Entra ID inconsistente ─────────────

    private async Task<(SemaforoColor, string)> EvaluarCC006Async(CancellationToken ct)
    {
        var ultimoSnapshot = await _db.SnapshotsEntraID
            .AsNoTracking()
            .OrderByDescending(s => s.FechaInstantanea)
            .FirstOrDefaultAsync(ct);

        if (ultimoSnapshot == null)
            return (SemaforoColor.NO_EVALUADO, "No hay snapshot de Entra ID para comparar.");

        var registrosEntraID = await _db.RegistrosEntraID
            .AsNoTracking()
            .Where(r => r.SnapshotId == ultimoSnapshot.Id && !string.IsNullOrWhiteSpace(r.EmployeeId))
            .Select(r => new { r.UserPrincipalName, r.EmployeeId, r.EmployeeIdNormalizado })
            .ToListAsync(ct);

        var sapUsers = await _db.UsuariosSistema
            .AsNoTracking()
            .Where(u => u.Sistema == "SAP" && u.Estado == EstadoUsuario.ACTIVO && !u.IsDeleted
                     && !string.IsNullOrWhiteSpace(u.Email))
            .Select(u => new { u.NombreUsuario, u.Email, u.Cedula, u.CedulaNormalizada })
            .ToListAsync(ct);

        // Cruzar por email (UPN) — tomar primero en caso de UPNs duplicados
        var entraByEmail = registrosEntraID
            .Where(r => !string.IsNullOrWhiteSpace(r.UserPrincipalName))
            .GroupBy(r => r.UserPrincipalName!.ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.First().EmployeeIdNormalizado ?? IdentityNormalizationHelper.NormalizarCedula(g.First().EmployeeId));

        int inconsistentes = 0;
        foreach (var sap in sapUsers)
        {
            if (string.IsNullOrWhiteSpace(sap.Email)) continue;
            var emailKey = sap.Email.ToLowerInvariant();
            if (!entraByEmail.TryGetValue(emailKey, out var empIdNorm)) continue;
            var cedSapNorm = sap.CedulaNormalizada ?? IdentityNormalizationHelper.NormalizarCedula(sap.Cedula);
            if (string.IsNullOrWhiteSpace(cedSapNorm) || string.IsNullOrWhiteSpace(empIdNorm)) continue;
            if (cedSapNorm != empIdNorm) inconsistentes++;
        }

        _logger.LogInformation("CC-006: {N} inconsistencias cédula SAP vs EmployeeId Entra ID", inconsistentes);

        return inconsistentes == 0
            ? (SemaforoColor.VERDE, "No se detectaron inconsistencias entre cédulas SAP y EmployeeId de Entra ID.")
            : (SemaforoColor.ROJO, $"CRÍTICO: {inconsistentes} usuario(s) con cédula SAP diferente al EmployeeId en Entra ID. Verificar integridad de identidad.");
    }

    // ─── CC-007: Usuario SAP con sociedad no permitida por la Matriz ──────────

    private async Task<(SemaforoColor, string)> EvaluarCC007Async(CancellationToken ct)
    {
        var sapUsers = await _db.UsuariosSistema
            .AsNoTracking()
            .Where(u => u.Sistema == "SAP" && u.Estado == EstadoUsuario.ACTIVO
                     && !u.IsDeleted && !string.IsNullOrWhiteSpace(u.Puesto))
            .Select(u => new { u.NombreUsuario, u.Puesto, u.Sociedad })
            .ToListAsync(ct);

        if (!sapUsers.Any())
            return (SemaforoColor.VERDE, "No hay usuarios SAP activos con Puesto registrado para evaluar.");

        var matrizCombos = await _db.MatrizPuestosSAP
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .Select(m => new { Puesto = m.Puesto.ToUpper(), Sociedad = m.Sociedad != null ? m.Sociedad.ToUpper() : null })
            .Distinct()
            .ToListAsync(ct);

        var combosPermitidos = matrizCombos
            .Select(m => $"{m.Puesto}|{m.Sociedad}")
            .ToHashSet();

        int noPermitidos = sapUsers.Count(u =>
        {
            if (string.IsNullOrWhiteSpace(u.Puesto)) return false;
            var puestoNorm = u.Puesto.ToUpperInvariant();
            var socNorm = string.IsNullOrWhiteSpace(u.Sociedad) ? null : u.Sociedad.ToUpperInvariant();
            // Si la Matriz no registra ningún combo para este puesto+sociedad, es no permitido
            return !combosPermitidos.Contains($"{puestoNorm}|{socNorm}")
                && !combosPermitidos.Contains($"{puestoNorm}|");
        });

        _logger.LogInformation("CC-007: {N} usuarios SAP con sociedad no en Matriz de Puestos", noPermitidos);

        return noPermitidos == 0
            ? (SemaforoColor.VERDE, "Todos los usuarios SAP tienen Puesto/Sociedad registrado en la Matriz de Puestos.")
            : noPermitidos <= 5
                ? (SemaforoColor.AMARILLO, $"{noPermitidos} usuario(s) SAP con combinación Puesto/Sociedad no documentada en la Matriz. Actualizar Matriz o revisar asignación.")
                : (SemaforoColor.ROJO, $"{noPermitidos} usuarios SAP con sociedad no autorizada por la Matriz de Puestos. Revisar accesos.");
    }

    // ─── CC-008: Rol SAP no permitido por la Matriz para el puesto/sociedad ───

    private async Task<(SemaforoColor, string)> EvaluarCC008Async(CancellationToken ct)
    {
        var asignaciones = await _db.AsignacionesRol
            .AsNoTracking()
            .Where(a => a.Activa)
            .Select(a => new
            {
                UsuarioNombre = a.Usuario.NombreUsuario,
                a.Usuario.Puesto,
                a.Usuario.Sociedad,
                RolNombre = a.Rol.NombreRol,
                a.CasoSESuiteRef
            })
            .ToListAsync(ct);

        if (!asignaciones.Any())
            return (SemaforoColor.VERDE, "No hay asignaciones de roles SAP para evaluar.");

        var matrizRoles = await _db.MatrizPuestosSAP
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .Select(m => new
            {
                Puesto = m.Puesto.ToUpper(),
                Rol = m.Rol.ToUpper(),
                Sociedad = m.Sociedad != null ? m.Sociedad.ToUpper() : null
            })
            .Distinct()
            .ToListAsync(ct);

        var combosMatriz = matrizRoles
            .Select(m => $"{m.Puesto}|{m.Rol}|{m.Sociedad}")
            .ToHashSet();

        int noPermitidos = 0;
        foreach (var asig in asignaciones)
        {
            if (string.IsNullOrWhiteSpace(asig.Puesto) || string.IsNullOrWhiteSpace(asig.RolNombre)) continue;
            var puestoNorm = asig.Puesto.ToUpperInvariant();
            var rolNorm = asig.RolNombre.ToUpperInvariant();
            var socNorm = string.IsNullOrWhiteSpace(asig.Sociedad) ? null : asig.Sociedad.ToUpperInvariant();

            var permitido = combosMatriz.Contains($"{puestoNorm}|{rolNorm}|{socNorm}")
                         || combosMatriz.Contains($"{puestoNorm}|{rolNorm}|");

            if (!permitido)
            {
                // Verificar excepción SE Suite vigente
                if (!string.IsNullOrWhiteSpace(asig.CasoSESuiteRef))
                {
                    var tieneExcepcion = await VerificarExcepcionSESuiteAsync(
                        asig.UsuarioNombre, rolNombre: asig.RolNombre, ct: ct);
                    if (tieneExcepcion) continue;
                }
                noPermitidos++;
            }
        }

        _logger.LogInformation("CC-008: {N} roles SAP no permitidos por la Matriz", noPermitidos);

        return noPermitidos == 0
            ? (SemaforoColor.VERDE, "Todos los roles SAP asignados están autorizados por la Matriz de Puestos o tienen excepción SE Suite vigente.")
            : noPermitidos <= 3
                ? (SemaforoColor.AMARILLO, $"{noPermitidos} rol(es) SAP fuera de la Matriz de Puestos sin excepción válida. Revisar y documentar.")
                : (SemaforoColor.ROJO, $"{noPermitidos} roles SAP no autorizados por la Matriz. Se requiere acción correctiva o actualización de la Matriz.");
    }

    // ─── CC-009: Transacción SAP no permitida por la Matriz ───────────────────

    private async Task<(SemaforoColor, string)> EvaluarCC009Async(CancellationToken ct)
    {
        var asignaciones = await _db.AsignacionesRol
            .AsNoTracking()
            .Where(a => a.Activa && !string.IsNullOrWhiteSpace(a.Rol.TransaccionesAutorizadas))
            .Select(a => new
            {
                a.Usuario.NombreUsuario,
                a.Usuario.Puesto,
                a.Usuario.Sociedad,
                RolNombre = a.Rol.NombreRol,
                TransaccionesRol = a.Rol.TransaccionesAutorizadas,
                a.CasoSESuiteRef
            })
            .ToListAsync(ct);

        if (!asignaciones.Any())
            return (SemaforoColor.VERDE, "No hay asignaciones con transacciones para evaluar.");

        var matrizTransacciones = await _db.MatrizPuestosSAP
            .AsNoTracking()
            .Where(m => !m.IsDeleted && !string.IsNullOrWhiteSpace(m.Transaccion))
            .Select(m => new
            {
                Puesto = m.Puesto.ToUpper(),
                Rol = m.Rol.ToUpper(),
                Transaccion = m.Transaccion!.ToUpper(),
                Sociedad = m.Sociedad != null ? m.Sociedad.ToUpper() : null
            })
            .ToListAsync(ct);

        var combosMatrizTcode = matrizTransacciones
            .Select(m => $"{m.Puesto}|{m.Rol}|{m.Transaccion}|{m.Sociedad}")
            .ToHashSet();

        int noPermitidos = 0;
        foreach (var asig in asignaciones)
        {
            if (string.IsNullOrWhiteSpace(asig.Puesto) || string.IsNullOrWhiteSpace(asig.TransaccionesRol)) continue;
            var puestoNorm = asig.Puesto.ToUpperInvariant();
            var rolNorm = asig.RolNombre.ToUpperInvariant();
            var socNorm = string.IsNullOrWhiteSpace(asig.Sociedad) ? null : asig.Sociedad.ToUpperInvariant();

            var tcodes = asig.TransaccionesRol!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var tc in tcodes)
            {
                var tcNorm = tc.ToUpperInvariant();
                var permitido = combosMatrizTcode.Contains($"{puestoNorm}|{rolNorm}|{tcNorm}|{socNorm}")
                             || combosMatrizTcode.Contains($"{puestoNorm}|{rolNorm}|{tcNorm}|");

                if (!permitido)
                {
                    // Verificar excepción SE Suite
                    if (!string.IsNullOrWhiteSpace(asig.CasoSESuiteRef))
                    {
                        var tieneExcepcion = await VerificarExcepcionSESuiteAsync(
                            asig.NombreUsuario, rolNombre: asig.RolNombre, transaccion: tc, ct: ct);
                        if (tieneExcepcion) continue;
                    }
                    noPermitidos++;
                }
            }
        }

        _logger.LogInformation("CC-009: {N} transacciones SAP no en Matriz", noPermitidos);

        return noPermitidos == 0
            ? (SemaforoColor.VERDE, "Todas las transacciones SAP están autorizadas en la Matriz de Puestos o tienen excepción SE Suite.")
            : noPermitidos <= 5
                ? (SemaforoColor.AMARILLO, $"{noPermitidos} transacción(es) SAP fuera de la Matriz. Verificar con Contraloría.")
                : (SemaforoColor.ROJO, $"{noPermitidos} transacciones SAP no autorizadas por la Matriz de Puestos. Riesgo de acceso indebido.");
    }

    // ─── CC-010: Fecha de revisión de Contraloría vencida ─────────────────────

    private async Task<(SemaforoColor, string)> EvaluarCC010Async(CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var count = await _db.MatrizPuestosSAP
            .AsNoTracking()
            .Where(m => !m.IsDeleted
                     && m.FechaRevisionContraloria.HasValue
                     && m.FechaRevisionContraloria.Value < hoy)
            .Select(m => m.Puesto)
            .Distinct()
            .CountAsync(ct);

        _logger.LogInformation("CC-010: {N} puestos con fecha revisión Contraloría vencida", count);

        return count == 0
            ? (SemaforoColor.VERDE, "La Matriz de Puestos está dentro de la vigencia de revisión de Contraloría.")
            : count <= 3
                ? (SemaforoColor.AMARILLO, $"{count} puesto(s) con Fecha de Revisión de Contraloría vencida. Solicitar actualización.")
                : (SemaforoColor.ROJO, $"{count} puestos con Matriz desactualizada por revisión de Contraloría vencida. Riesgo de incumplimiento.");
    }

    // ─── CC-011: Rol crítico SAP sin expediente o justificación ──────────────

    private async Task<(SemaforoColor, string)> EvaluarCC011Async(int? sociedadId, CancellationToken ct)
    {
        var rolesCriticos = await _db.RolesSistema
            .AsNoTracking()
            .Where(r => r.Sistema == "SAP" && r.EsCritico && r.Activo)
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (!rolesCriticos.Any())
            return (SemaforoColor.VERDE, "No hay roles SAP marcados como críticos en el sistema.");

        var sinExpediente = await _db.AsignacionesRol
            .AsNoTracking()
            .Where(a => rolesCriticos.Contains(a.RolId) && a.Activa
                     && string.IsNullOrWhiteSpace(a.ExpedienteRef)
                     && string.IsNullOrWhiteSpace(a.CasoSESuiteRef))
            .CountAsync(ct);

        _logger.LogInformation("CC-011: {N} asignaciones de roles críticos SAP sin expediente", sinExpediente);

        return sinExpediente == 0
            ? (SemaforoColor.VERDE, "Todas las asignaciones de roles críticos SAP tienen expediente o justificación.")
            : (SemaforoColor.ROJO, $"{sinExpediente} asignación(es) de rol(es) crítico(s) SAP sin expediente ni caso SE Suite. Requiere justificación inmediata.");
    }

    // ─── CC-012: Posible conflicto SoD (Segregación de Funciones) ────────────

    private async Task<(SemaforoColor, string)> EvaluarCC012Async(int? sociedadId, CancellationToken ct)
    {
        // Contar conflictos SoD activos de riesgo CRITICO o ALTO
        var conflictos = await _db.ConflictosSoD
            .AsNoTracking()
            .Where(c => c.Activo && (c.Riesgo == "CRITICO" || c.Riesgo == "ALTO"))
            .CountAsync(ct);

        // Adicionalmente, verificar si hay usuarios con ambos roles del conflicto asignados
        var conflictosActivos = await _db.ConflictosSoD
            .AsNoTracking()
            .Where(c => c.Activo && c.RolAId.HasValue && c.RolBId.HasValue)
            .Select(c => new { c.RolAId, c.RolBId, c.Riesgo })
            .ToListAsync(ct);

        int usuariosConConflicto = 0;
        foreach (var conf in conflictosActivos)
        {
            var usuariosRolA = await _db.AsignacionesRol
                .AsNoTracking()
                .Where(a => a.RolId == conf.RolAId && a.Activa)
                .Select(a => a.UsuarioId)
                .ToListAsync(ct);

            var usuariosRolB = await _db.AsignacionesRol
                .AsNoTracking()
                .Where(a => a.RolId == conf.RolBId && a.Activa)
                .Select(a => a.UsuarioId)
                .ToListAsync(ct);

            usuariosConConflicto += usuariosRolA.Intersect(usuariosRolB).Count();
        }

        _logger.LogInformation("CC-012: {C} definiciones SoD activas, {U} usuarios con conflicto real", conflictos, usuariosConConflicto);

        if (usuariosConConflicto == 0 && conflictos == 0)
            return (SemaforoColor.VERDE, "No se detectaron conflictos de Segregación de Funciones activos.");

        if (usuariosConConflicto == 0)
            return (SemaforoColor.AMARILLO, $"{conflictos} definición(es) SoD de riesgo crítico/alto registradas. Ningún usuario activo las activa actualmente.");

        return usuariosConConflicto <= 2
            ? (SemaforoColor.AMARILLO, $"{usuariosConConflicto} usuario(s) con conflicto SoD activo. Revisar y documentar mitigación.")
            : (SemaforoColor.ROJO, $"CRÍTICO: {usuariosConConflicto} usuario(s) con roles incompatibles asignados simultáneamente (SoD). Acción inmediata.");
    }

    // ─── Helper SE Suite: verificar excepción vigente ─────────────────────────

    private async Task<bool> VerificarExcepcionSESuiteAsync(
        string usuarioSAP,
        string? rolNombre = null,
        string? transaccion = null,
        CancellationToken ct = default)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        // Buscar caso SE Suite vigente y aprobado para este usuario/rol/transaccion
        var query = _db.CasosSESuite
            .AsNoTracking()
            .Where(c => !c.IsDeleted
                     && c.EstadoCaso == "APROBADO"
                     && (c.FechaVencimiento == null || c.FechaVencimiento >= hoy)
                     && c.UsuarioSAP == usuarioSAP.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(rolNombre))
            query = query.Where(c => c.RolJustificado != null &&
                c.RolJustificado.ToUpper() == rolNombre.ToUpperInvariant());

        var casos = await query.ToListAsync(ct);
        if (!casos.Any()) return false;

        if (!string.IsNullOrWhiteSpace(transaccion))
        {
            // Verificar que la transacción esté en las justificadas del caso
            return casos.Any(c =>
                !string.IsNullOrWhiteSpace(c.TransaccionesJustificadas) &&
                c.TransaccionesJustificadas
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(t => t.Equals(transaccion, StringComparison.OrdinalIgnoreCase)));
        }

        return true;
    }

    // ─── Generadores ──────────────────────────────────────────────────────────

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

        var evaluados = resultados.Where(r => r.Semaforo != SemaforoColor.NO_EVALUADO).ToList();
        if (!evaluados.Any()) return (1, 0);

        var total = evaluados.Count;
        var verdes = evaluados.Count(r => r.Semaforo == SemaforoColor.VERDE);
        var amarillos = evaluados.Count(r => r.Semaforo == SemaforoColor.AMARILLO);

        var pctVerdes = (double)verdes / total;
        var pctAmarillos = (double)amarillos / total;
        var hallazgosCriticos = hallazgos.Count(h => h.Criticidad == Criticidad.CRITICA);

        var score = (pctVerdes * 4.0) + (pctAmarillos * 2.0) + (0.6 * 2.0);
        score -= Math.Min(hallazgosCriticos * 0.1, 0.5);
        score = Math.Max(1.0, Math.Min(10.0, score));

        return ((decimal)Math.Round(score, 1), Math.Round((decimal)(pctVerdes * 100), 1));
    }
}
