using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AuditorPRO.Application.Features.Simulaciones;

// --- Commands ---

public record IniciarSimulacionCommand(
    string Nombre,
    string? Descripcion,
    TipoSimulacion Tipo,
    List<int> SociedadIds,
    DateOnly PeriodoInicio,
    DateOnly PeriodoFin,
    List<int>? DominioIds,
    List<int>? PuntosControlIds
) : IRequest<Guid>;

public class IniciarSimulacionValidator : AbstractValidator<IniciarSimulacionCommand>
{
    public IniciarSimulacionValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(300);
        RuleFor(x => x.SociedadIds).NotEmpty().WithMessage("Debe seleccionar al menos una sociedad.");
        RuleFor(x => x.PeriodoInicio).LessThanOrEqualTo(x => x.PeriodoFin);
    }
}

public class IniciarSimulacionHandler : IRequestHandler<IniciarSimulacionCommand, Guid>
{
    private readonly ISimulacionRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLoggerService _auditLogger;

    public IniciarSimulacionHandler(ISimulacionRepository repo, ICurrentUserService currentUser, IAuditLoggerService auditLogger)
    {
        _repo = repo;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Guid> Handle(IniciarSimulacionCommand request, CancellationToken cancellationToken)
    {
        var simulacion = new SimulacionAuditoria
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Tipo = request.Tipo,
            Estado = EstadoSimulacion.PENDIENTE,
            SociedadIds = System.Text.Json.JsonSerializer.Serialize(request.SociedadIds),
            PeriodoInicio = request.PeriodoInicio,
            PeriodoFin = request.PeriodoFin,
            DominioIds = request.DominioIds != null ? System.Text.Json.JsonSerializer.Serialize(request.DominioIds) : null,
            PuntosControlIds = request.PuntosControlIds != null ? System.Text.Json.JsonSerializer.Serialize(request.PuntosControlIds) : null,
            IniciadaPor = _currentUser.Email,
            CreatedBy = _currentUser.Email
        };

        await _repo.AddAsync(simulacion, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(_currentUser.UserId, _currentUser.Email,
            "EJECUTAR_SIMULACION", "SimulacionAuditoria", simulacion.Id.ToString(), ct: cancellationToken);

        return simulacion.Id;
    }
}

public record CancelarSimulacionCommand(Guid SimulacionId) : IRequest;

public class CancelarSimulacionHandler : IRequestHandler<CancelarSimulacionCommand>
{
    private readonly ISimulacionRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public CancelarSimulacionHandler(ISimulacionRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task Handle(CancelarSimulacionCommand request, CancellationToken cancellationToken)
    {
        var sim = await _repo.GetByIdAsync(request.SimulacionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Simulación {request.SimulacionId} no encontrada.");

        if (sim.Estado == EstadoSimulacion.COMPLETADA)
            throw new InvalidOperationException("No se puede cancelar una simulación completada.");

        sim.Estado = EstadoSimulacion.CANCELADA;
        await _repo.UpdateAsync(sim, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);
    }
}

// ─── Motor de Control Cruzado ─────────────────────────────────────────────────
// Ejecuta las reglas R01–R08 sobre los datos cargados y genera Hallazgos

public record EjecutarControlCruzadoCommand(
    Guid SimulacionId,
    /// <summary>Objetivo / comentario del auditor para esta ejecución</summary>
    string? Objetivo = null,
    /// <summary>COMPLETO | SAP_NOMINA | SAP_ENTRA_ID | SOD_ONLY</summary>
    string TipoControlCruzado = "COMPLETO"
) : IRequest<ControlCruzadoResultado>;

public record ControlCruzadoResultado(
    int TotalHallazgos,
    int Criticos,
    int Medios,
    int Bajos,
    Dictionary<string, int> PorRegla
);

public class EjecutarControlCruzadoHandler : IRequestHandler<EjecutarControlCruzadoCommand, ControlCruzadoResultado>
{
    private readonly ISimulacionRepository _simRepo;
    private readonly IRepository<UsuarioSistema> _usuarioRepo;
    private readonly IRepository<EmpleadoMaestro> _empleadoRepo;
    private readonly IRepository<MatrizPuestoSAP> _matrizRepo;
    private readonly IRepository<CasoSESuite> _casoRepo;
    private readonly IRepository<AsignacionRolUsuario> _asignRepo;
    private readonly IRepository<RolSistema> _rolRepo;
    private readonly IRepository<ConflictoSoD> _sodRepo;
    private readonly IRepository<SnapshotEntraID> _snapshotRepo;
    private readonly IRepository<RegistroEntraID> _registroEntraRepo;
    private readonly IHallazgoRepository _hallazgoRepo;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;

    public EjecutarControlCruzadoHandler(
        ISimulacionRepository simRepo,
        IRepository<UsuarioSistema> usuarioRepo,
        IRepository<EmpleadoMaestro> empleadoRepo,
        IRepository<MatrizPuestoSAP> matrizRepo,
        IRepository<CasoSESuite> casoRepo,
        IRepository<AsignacionRolUsuario> asignRepo,
        IRepository<RolSistema> rolRepo,
        IRepository<ConflictoSoD> sodRepo,
        IRepository<SnapshotEntraID> snapshotRepo,
        IRepository<RegistroEntraID> registroEntraRepo,
        IHallazgoRepository hallazgoRepo,
        ICurrentUserService user,
        IAuditLoggerService audit)
    {
        _simRepo = simRepo; _usuarioRepo = usuarioRepo; _empleadoRepo = empleadoRepo;
        _matrizRepo = matrizRepo; _casoRepo = casoRepo; _asignRepo = asignRepo;
        _rolRepo = rolRepo; _sodRepo = sodRepo;
        _snapshotRepo = snapshotRepo; _registroEntraRepo = registroEntraRepo;
        _hallazgoRepo = hallazgoRepo; _user = user; _audit = audit;
    }

    public async Task<ControlCruzadoResultado> Handle(EjecutarControlCruzadoCommand request, CancellationToken ct)
    {
        var sim = await _simRepo.GetByIdAsync(request.SimulacionId, ct)
            ?? throw new KeyNotFoundException($"Simulación {request.SimulacionId} no encontrada.");

        sim.Estado = EstadoSimulacion.EN_PROCESO;
        sim.Objetivo = request.Objetivo;
        sim.TipoSimulacion = request.TipoControlCruzado;
        await _simRepo.UpdateAsync(sim, ct);
        await _simRepo.SaveChangesAsync(ct);

        // ── Cargar datos ────────────────────────────────────────────────────
        var usuarios    = (await _usuarioRepo.GetAllAsync(ct)).ToList();
        var empleados   = (await _empleadoRepo.GetAllAsync(ct)).ToList();
        var matrizRows  = (await _matrizRepo.GetAllAsync(ct)).ToList();
        var casos       = (await _casoRepo.GetAllAsync(ct)).ToList();
        var asignaciones = (await _asignRepo.GetAllAsync(ct)).ToList();
        var roles       = (await _rolRepo.GetAllAsync(ct)).ToDictionary(r => r.Id);
        var conflictos  = (await _sodRepo.GetAllAsync(ct)).ToList();

        // ── Cargar snapshot Entra ID más reciente ───────────────────────────
        var snapshots      = (await _snapshotRepo.GetAllAsync(ct)).ToList();
        var latestSnapshot = snapshots.OrderByDescending(s => s.FechaInstantanea).FirstOrDefault();
        List<RegistroEntraID> registrosEntra = [];
        if (latestSnapshot != null)
        {
            registrosEntra = (await _registroEntraRepo
                .FindAsync(r => r.SnapshotId == latestSnapshot.Id, ct)).ToList();
        }

        var hallazgos = new List<Hallazgo>();
        var porRegla  = new Dictionary<string, int>();
        var hoy       = DateOnly.FromDateTime(DateTime.UtcNow);

        // Índices para performance
        // empleado por cédula
        var empPorCedula = empleados
            .Where(e => !string.IsNullOrWhiteSpace(e.Cedula))
            .GroupBy(e => e.Cedula!.Trim())
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // empleado por número de empleado
        var empPorNumero = empleados.ToDictionary(e => e.NumeroEmpleado, StringComparer.OrdinalIgnoreCase);

        // usuario SAP por NombreUsuario
        var usrSAP = usuarios
            .Where(u => u.Sistema == "SAP" && u.Estado == EstadoUsuario.ACTIVO)
            .ToDictionary(u => u.NombreUsuario.ToUpperInvariant());

        // Asignaciones de roles por usuarioId
        var asignPorUsuario = asignaciones
            .Where(a => a.Activa)
            .GroupBy(a => a.UsuarioId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Matriz indexada por (puesto, rol) — conjunto de combos autorizados
        var matrizAutorizada = matrizRows
            .Select(m => (Puesto: m.Puesto?.Trim().ToUpperInvariant() ?? "",
                          Rol: m.Rol.Trim().ToUpperInvariant()))
            .ToHashSet();

        // Casos SE Suite vigentes por (usuarioSAP, rol)
        var casosVigentes = casos
            .Where(c => c.EstadoCaso == "APROBADO" &&
                        (c.FechaVencimiento == null || c.FechaVencimiento >= hoy))
            .GroupBy(c => (c.UsuarioSAP?.ToUpperInvariant() ?? "", c.RolJustificado?.ToUpperInvariant() ?? ""))
            .ToDictionary(g => g.Key, g => g.First());

        // Entra ID: índice por EmployeeId (cédula) del snapshot más reciente
        var entraIDPorCedula = registrosEntra
            .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeId))
            .GroupBy(r => r.EmployeeId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var fechaSnapshot = latestSnapshot?.FechaInstantanea.ToString("dd/MM/yyyy HH:mm") ?? "—";

        // ── R01: Ex-empleado con usuario activo en SAP ───────────────────────
        if (request.TipoControlCruzado is "COMPLETO" or "SAP_NOMINA")
        {
            int r01 = 0;
            foreach (var usr in usrSAP.Values)
            {
                EmpleadoMaestro? emp = null;
                if (!string.IsNullOrWhiteSpace(usr.Cedula))
                    empPorCedula.TryGetValue(usr.Cedula.Trim(), out emp);
                if (emp == null && !string.IsNullOrWhiteSpace(usr.NombreUsuario))
                    empPorNumero.TryGetValue(usr.NombreUsuario, out emp);

                if (emp != null && (emp.EstadoLaboral == EstadoLaboral.BAJA_PROCESADA ||
                                    emp.EstadoLaboral == EstadoLaboral.INACTIVO))
                {
                    var h = CrearHallazgo(sim.Id, "R01", "ACCESO_EX_EMPLEADO",
                        $"Ex-empleado con usuario SAP activo: {usr.NombreUsuario}",
                        $"El empleado '{emp.NombreCompleto}' (cédula: {emp.Cedula}) tiene estado laboral {emp.EstadoLaboral} " +
                        $"pero su usuario SAP '{usr.NombreUsuario}' sigue ACTIVO.",
                        Criticidad.CRITICA, usr.Cedula, usr.NombreUsuario);
                    hallazgos.Add(h); r01++;
                }
            }
            porRegla["R01_EX_EMPLEADO"] = r01;
        }

        // ── R02: Conflicto SoD ────────────────────────────────────────────────
        if (request.TipoControlCruzado is "COMPLETO" or "SOD_ONLY")
        {
            int r02 = 0;
            var conflictoIndex = conflictos
                .Where(c => c.Activo && c.RolAId.HasValue && c.RolBId.HasValue)
                .Select(c => (A: c.RolAId!.Value, B: c.RolBId!.Value, Desc: c.Descripcion, Riesgo: c.Riesgo))
                .ToList();

            foreach (var (usrId, asigns) in asignPorUsuario)
            {
                var rolIds = asigns.Select(a => a.RolId).ToHashSet();
                var usr = usuarios.FirstOrDefault(u => u.Id == usrId);
                if (usr == null) continue;

                foreach (var conf in conflictoIndex)
                {
                    if (rolIds.Contains(conf.A) && rolIds.Contains(conf.B))
                    {
                        var rolA = roles.GetValueOrDefault(conf.A);
                        var rolB = roles.GetValueOrDefault(conf.B);
                        var h = CrearHallazgo(sim.Id, "R02", "CONFLICTO_SOD",
                            $"SoD: {rolA?.NombreRol} + {rolB?.NombreRol} — usuario {usr.NombreUsuario}",
                            $"Usuario '{usr.NombreUsuario}' tiene simultáneamente los roles " +
                            $"'{rolA?.NombreRol}' y '{rolB?.NombreRol}' que presentan conflicto de segregación de funciones. " +
                            $"Riesgo: {conf.Riesgo}. {conf.Desc}",
                            Criticidad.CRITICA, usr.Cedula, usr.NombreUsuario,
                            rolAfectado: $"{rolA?.NombreRol}|{rolB?.NombreRol}");
                        hallazgos.Add(h); r02++;
                    }
                }
            }
            porRegla["R02_SOD"] = r02;
        }

        // ── R03: Rol fuera de Matriz de Puestos sin caso SE Suite vigente ─────
        if (request.TipoControlCruzado is "COMPLETO" or "SAP_NOMINA" && matrizRows.Count > 0)
        {
            int r03 = 0;
            foreach (var (usrId, asigns) in asignPorUsuario)
            {
                var usr = usuarios.FirstOrDefault(u => u.Id == usrId && u.Sistema == "SAP");
                if (usr == null) continue;

                var puestoUsr = usr.Puesto?.Trim().ToUpperInvariant() ?? "";

                foreach (var asign in asigns)
                {
                    var rol = roles.GetValueOrDefault(asign.RolId);
                    if (rol == null) continue;
                    var rolNom = rol.NombreRol.Trim().ToUpperInvariant();

                    // ¿Está el (puesto, rol) en la Matriz autorizada?
                    if (!string.IsNullOrWhiteSpace(puestoUsr) &&
                        !matrizAutorizada.Contains((puestoUsr, rolNom)))
                    {
                        // ¿Hay caso SE Suite vigente que lo justifique?
                        var clave = (usr.NombreUsuario.ToUpperInvariant(), rolNom);
                        if (!casosVigentes.ContainsKey(clave))
                        {
                            var h = CrearHallazgo(sim.Id, "R03", "ROL_NO_AUTORIZADO_MATRIZ",
                                $"Rol no autorizado para el puesto: {usr.NombreUsuario} / {rol.NombreRol}",
                                $"El usuario '{usr.NombreUsuario}' (puesto: {usr.Puesto}) tiene asignado el rol " +
                                $"'{rol.NombreRol}' que NO figura en la Matriz de Puestos aprobada por Contraloría " +
                                $"y no existe un caso SE Suite vigente que lo justifique.",
                                rol.EsCritico ? Criticidad.CRITICA : Criticidad.MEDIA,
                                usr.Cedula, usr.NombreUsuario, rolAfectado: rol.NombreRol);
                            hallazgos.Add(h); r03++;
                        }
                    }
                }
            }
            porRegla["R03_ROL_FUERA_MATRIZ"] = r03;
        }

        // ── R04: Caso SE Suite vencido — usuario sigue con el rol ─────────────
        if (request.TipoControlCruzado is "COMPLETO" or "SAP_NOMINA")
        {
            int r04 = 0;
            var casosVencidos = casos
                .Where(c => c.FechaVencimiento.HasValue && c.FechaVencimiento < hoy &&
                            c.EstadoCaso != "ANULADO")
                .ToList();

            foreach (var caso in casosVencidos)
            {
                var usrKey = caso.UsuarioSAP?.ToUpperInvariant() ?? "";
                if (!usrSAP.TryGetValue(usrKey, out var usr)) continue;

                var rolNom = caso.RolJustificado?.ToUpperInvariant() ?? "";
                if (!asignPorUsuario.TryGetValue(usr.Id, out var asignsUsr)) continue;

                bool tieneRol = asignsUsr.Any(a =>
                    roles.TryGetValue(a.RolId, out var r) &&
                    r.NombreRol.ToUpperInvariant() == rolNom);

                if (tieneRol)
                {
                    var h = CrearHallazgo(sim.Id, "R04", "CASO_SESUITE_VENCIDO",
                        $"Caso SE Suite vencido: {caso.NumeroCaso} — {usr.NombreUsuario}",
                        $"El caso SE Suite '{caso.NumeroCaso}' (rol: {caso.RolJustificado}) venció el " +
                        $"{caso.FechaVencimiento:dd/MM/yyyy} pero el usuario '{usr.NombreUsuario}' " +
                        $"sigue con el rol asignado. Se requiere renovación o eliminación del acceso.",
                        Criticidad.MEDIA, usr.Cedula, usr.NombreUsuario,
                        rolAfectado: caso.RolJustificado, casoSESuiteRef: caso.NumeroCaso);
                    hallazgos.Add(h); r04++;
                }
            }
            porRegla["R04_CASO_VENCIDO"] = r04;
        }

        // ── R05: Empleado activo sin cuenta SAP ───────────────────────────────
        if (request.TipoControlCruzado is "COMPLETO" or "SAP_NOMINA"
            && empleados.Count > 0 && usrSAP.Count > 0)
        {
            int r05 = 0;
            // Solo evaluamos si hay matriz de puestos cargada (contexto de revisión de accesos)
            if (matrizRows.Count > 0)
            {
                var cedulasConSAP = usrSAP.Values
                    .Where(u => !string.IsNullOrWhiteSpace(u.Cedula))
                    .Select(u => u.Cedula!.Trim().ToUpperInvariant())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var emp in empleados.Where(e => e.EstadoLaboral == EstadoLaboral.ACTIVO))
                {
                    if (string.IsNullOrWhiteSpace(emp.Cedula)) continue;
                    if (!cedulasConSAP.Contains(emp.Cedula.Trim()))
                    {
                        // Empleado activo sin ninguna cuenta SAP activa vinculada
                        var h = CrearHallazgo(sim.Id, "R05", "EMPLEADO_SIN_CUENTA_SAP",
                            $"Empleado activo sin cuenta SAP: {emp.NombreCompleto}",
                            $"El empleado '{emp.NombreCompleto}' (cédula: {emp.Cedula}) está activo en nómina " +
                            $"pero no tiene ninguna cuenta de usuario SAP activa vinculada por cédula.",
                            Criticidad.BAJA, emp.Cedula, rolAfectado: null);
                        hallazgos.Add(h); r05++;
                    }
                }
            }
            porRegla["R05_SIN_CUENTA_SAP"] = r05;
        }

        // ── R06: Usuario SAP activo con cuenta Entra ID deshabilitada ─────────
        if (request.TipoControlCruzado is "COMPLETO" or "SAP_ENTRA_ID" && registrosEntra.Count > 0)
        {
            int r06 = 0;
            foreach (var usr in usrSAP.Values)
            {
                if (string.IsNullOrWhiteSpace(usr.Cedula)) continue;
                if (!entraIDPorCedula.TryGetValue(usr.Cedula.Trim(), out var reg)) continue;

                if (!reg.AccountEnabled)
                {
                    var h = CrearHallazgo(sim.Id, "R06", "SAP_ACTIVO_ENTRA_ID_DESHABILITADO",
                        $"SAP activo pero Entra ID deshabilitado: {usr.NombreUsuario}",
                        $"El usuario '{usr.NombreUsuario}' (cédula: {usr.Cedula}) tiene estado ACTIVO en SAP " +
                        $"pero su cuenta en Azure AD (Entra ID) está DESHABILITADA (AccountEnabled = false) " +
                        $"según el snapshot del {fechaSnapshot}. " +
                        $"UPN: {reg.UserPrincipalName ?? "—"}. Requiere revisión inmediata.",
                        Criticidad.CRITICA, usr.Cedula, usr.NombreUsuario);
                    hallazgos.Add(h); r06++;
                }
            }
            porRegla["R06_SAP_ACTIVO_ENTRA_ID_OFF"] = r06;
        }

        // ── R07: Ex-empleado con cuenta Entra ID habilitada ───────────────────
        if (request.TipoControlCruzado is "COMPLETO" or "SAP_ENTRA_ID" && registrosEntra.Count > 0)
        {
            int r07 = 0;
            foreach (var (cedula, reg) in entraIDPorCedula)
            {
                if (!reg.AccountEnabled) continue; // ya cubierto en R06
                if (!empPorCedula.TryGetValue(cedula, out var emp)) continue;

                if (emp.EstadoLaboral is EstadoLaboral.BAJA_PROCESADA or EstadoLaboral.INACTIVO)
                {
                    var h = CrearHallazgo(sim.Id, "R07", "EX_EMPLEADO_ENTRA_ID_ACTIVO",
                        $"Ex-empleado con Entra ID habilitado: {emp.NombreCompleto}",
                        $"El empleado '{emp.NombreCompleto}' (cédula: {cedula}) tiene estado laboral " +
                        $"{emp.EstadoLaboral} en nómina pero su cuenta Azure AD (Entra ID) sigue HABILITADA " +
                        $"(AccountEnabled = true) en el snapshot del {fechaSnapshot}. " +
                        $"UPN: {reg.UserPrincipalName ?? "—"}. Cuenta huérfana — riesgo de acceso no autorizado.",
                        Criticidad.CRITICA, cedula, rolAfectado: null);
                    hallazgos.Add(h); r07++;
                }
            }
            porRegla["R07_EX_EMPLEADO_ENTRA_ID_ON"] = r07;
        }

        // ── R08: Empleado activo sin cuenta en Entra ID ───────────────────────
        if (request.TipoControlCruzado is "COMPLETO" or "SAP_ENTRA_ID" && registrosEntra.Count > 0)
        {
            int r08 = 0;
            foreach (var emp in empleados.Where(e => e.EstadoLaboral == EstadoLaboral.ACTIVO))
            {
                if (string.IsNullOrWhiteSpace(emp.Cedula)) continue;
                if (!entraIDPorCedula.ContainsKey(emp.Cedula.Trim()))
                {
                    var h = CrearHallazgo(sim.Id, "R08", "EMPLEADO_SIN_CUENTA_ENTRA_ID",
                        $"Empleado activo sin cuenta Entra ID: {emp.NombreCompleto}",
                        $"El empleado '{emp.NombreCompleto}' (cédula: {emp.Cedula}) está activo en nómina " +
                        $"pero no aparece en el snapshot de Entra ID del {fechaSnapshot} " +
                        $"(buscado por campo EmployeeId = cédula). " +
                        $"Posible omisión de aprovisionamiento o discrepancia en el campo EmployeeId del directorio.",
                        Criticidad.BAJA, emp.Cedula, rolAfectado: null);
                    hallazgos.Add(h); r08++;
                }
            }
            porRegla["R08_SIN_CUENTA_ENTRA_ID"] = r08;
        }

        // ── Guardar hallazgos ─────────────────────────────────────────────────
        foreach (var h in hallazgos)
            await _hallazgoRepo.AddAsync(h, ct);

        if (hallazgos.Count > 0)
            await _hallazgoRepo.SaveChangesAsync(ct);

        // ── Actualizar simulación ─────────────────────────────────────────────
        int criticos = hallazgos.Count(h => h.Criticidad == Criticidad.CRITICA);
        int medios   = hallazgos.Count(h => h.Criticidad == Criticidad.MEDIA);
        int bajos    = hallazgos.Count(h => h.Criticidad == Criticidad.BAJA);

        sim.Estado = EstadoSimulacion.COMPLETADA;
        sim.CompletadaAt = DateTime.UtcNow;
        sim.TotalCriticos = criticos;
        sim.TotalControles = porRegla.Count;
        sim.ControlesRojo    = porRegla.Values.Count(v => v > 0);
        sim.ControlesVerde   = porRegla.Values.Count(v => v == 0);
        sim.ResumenResultados = System.Text.Json.JsonSerializer.Serialize(porRegla);
        await _simRepo.UpdateAsync(sim, ct);
        await _simRepo.SaveChangesAsync(ct);

        await _audit.LogAsync(_user.UserId, _user.Email, "CONTROL_CRUZADO_EJECUTADO",
            "SimulacionAuditoria", sim.Id.ToString(),
            datosDespues: new { TotalHallazgos = hallazgos.Count, Criticos = criticos },
            ct: ct);

        return new ControlCruzadoResultado(hallazgos.Count, criticos, medios, bajos, porRegla);
    }

    private static Hallazgo CrearHallazgo(
        Guid simId, string regla, string tipoHallazgo,
        string titulo, string descripcion, Criticidad criticidad,
        string? cedula = null, string? usuarioSAP = null,
        string? rolAfectado = null, string? casoSESuiteRef = null)
    {
        return new Hallazgo
        {
            SimulacionId = simId,
            Titulo       = $"[{regla}] {titulo}",
            Descripcion  = descripcion,
            Criticidad   = criticidad,
            Estado       = EstadoHallazgo.ABIERTO,
            TipoHallazgo = tipoHallazgo,
            Cedula       = cedula,
            UsuarioSAP   = usuarioSAP?.ToUpperInvariant(),
            RolAfectado  = rolAfectado,
            CasoSESuiteRef = casoSESuiteRef,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };
    }
}
