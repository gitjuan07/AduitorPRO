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

public record BorrarTodasSimulacionesCommand : IRequest<int>;

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
    private readonly IRepository<LoteCarga> _loteRepo;
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
        IRepository<LoteCarga> loteRepo,
        IHallazgoRepository hallazgoRepo,
        ICurrentUserService user,
        IAuditLoggerService audit)
    {
        _simRepo = simRepo; _usuarioRepo = usuarioRepo; _empleadoRepo = empleadoRepo;
        _matrizRepo = matrizRepo; _casoRepo = casoRepo; _asignRepo = asignRepo;
        _rolRepo = rolRepo; _sodRepo = sodRepo;
        _snapshotRepo = snapshotRepo; _registroEntraRepo = registroEntraRepo;
        _loteRepo = loteRepo; _hallazgoRepo = hallazgoRepo; _user = user; _audit = audit;
    }

    public async Task<ControlCruzadoResultado> Handle(EjecutarControlCruzadoCommand request, CancellationToken ct)
    {
        var sim = await _simRepo.GetByIdAsync(request.SimulacionId, ct)
            ?? throw new KeyNotFoundException($"Simulación {request.SimulacionId} no encontrada.");

        if (sim.Estado == EstadoSimulacion.COMPLETADA)
            throw new InvalidOperationException(
                "Esta simulación ya fue completada. Inicie una nueva simulación para ejecutar un nuevo análisis.");

        sim.Estado = EstadoSimulacion.EN_PROCESO;
        sim.Objetivo = request.Objetivo;
        sim.TipoSimulacion = request.TipoControlCruzado;
        await _simRepo.UpdateAsync(sim, ct);
        await _simRepo.SaveChangesAsync(ct);

        // ── Capturar lotes vigentes al momento de la ejecución ──────────────
        var todosLotes = await _loteRepo.GetAllAsync(ct);
        LoteCarga? LoteVigente(string tipo) => todosLotes
            .Where(l => l.TipoCarga == tipo && l.EsVigente)
            .OrderByDescending(l => l.FechaCarga)
            .FirstOrDefault();

        var loteSAP      = LoteVigente("SAP_ROLES");
        var loteMatriz   = LoteVigente("MATRIZ_PUESTOS");
        var loteEmpleados = LoteVigente("EMPLEADOS");
        var loteCasos    = LoteVigente("CASOS_SESUITE");
        var loteEntraID  = LoteVigente("ENTRA_ID");

        var lotesUsados = new Dictionary<string, string?>();
        if (loteSAP      != null) lotesUsados["SAP_ROLES"]       = loteSAP.Id.ToString();
        if (loteMatriz   != null) lotesUsados["MATRIZ_PUESTOS"]  = loteMatriz.Id.ToString();
        if (loteEmpleados!= null) lotesUsados["EMPLEADOS"]       = loteEmpleados.Id.ToString();
        if (loteCasos    != null) lotesUsados["CASOS_SESUITE"]   = loteCasos.Id.ToString();
        if (loteEntraID  != null) lotesUsados["ENTRA_ID"]        = loteEntraID.Id.ToString();

        // La fecha de referencia es la más antigua de los lotes usados (carga más vieja que influye)
        var fechasLotes = new[] { loteSAP, loteMatriz, loteEmpleados, loteCasos, loteEntraID }
            .Where(l => l != null).Select(l => l!.FechaCarga).ToList();
        var fechaRefDatos = fechasLotes.Count > 0 ? fechasLotes.Min() : (DateTime?)null;

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
        sim.LotesUsadosJson  = System.Text.Json.JsonSerializer.Serialize(lotesUsados);
        sim.FechaReferenciaDatos = fechaRefDatos;
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
        var (norma, riesgo, plan) = MetadatosPorTipo(tipoHallazgo, criticidad, usuarioSAP, rolAfectado);
        return new Hallazgo
        {
            SimulacionId   = simId,
            Titulo         = $"[{regla}] {titulo}",
            Descripcion    = descripcion,
            Criticidad     = criticidad,
            Estado         = EstadoHallazgo.ABIERTO,
            TipoHallazgo   = tipoHallazgo,
            Cedula         = cedula,
            UsuarioSAP     = usuarioSAP?.ToUpperInvariant(),
            RolAfectado    = rolAfectado,
            CasoSESuiteRef = casoSESuiteRef,
            NormaAfectada  = norma,
            RiesgoAsociado = riesgo,
            PlanAccion     = plan,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
        };
    }

    // ─── Metadatos normativos + plan recomendado (estilo Deloitte) ────────────
    private static (string norma, string riesgo, string plan) MetadatosPorTipo(
        string tipoHallazgo, Criticidad criticidad, string? usuarioSAP, string? rolAfectado)
    {
        var uSAP  = usuarioSAP  ?? "el usuario afectado";
        var rol   = rolAfectado ?? "el rol afectado";
        var plazo = criticidad == Criticidad.CRITICA ? "24-48 horas (URGENTE)"
                  : criticidad == Criticidad.MEDIA   ? "5 días hábiles"
                                                     : "15 días hábiles";

        return tipoHallazgo switch
        {
            "ACCESO_EX_EMPLEADO" => (
                norma:  "ISO 27001:2022 A.5.18 / COBIT 2019 APO01.03 / SOX ITGC UC-1",
                riesgo: "Acceso no autorizado a sistemas críticos por personal externo. "
                      + "Riesgo de fraude, exfiltración de datos y responsabilidad regulatoria. "
                      + "Incumplimiento de política de revocación de accesos al cese laboral.",
                plan:   $"OBSERVACIÓN: Usuario SAP {uSAP} permanece activo en el sistema pese a haber sido dado de baja en nómina.\n\n"
                      + "CAUSA RAÍZ: Ausencia de proceso automatizado de revocación de accesos integrado con el sistema de RRHH al momento de la terminación laboral.\n\n"
                      + "ACCIONES REQUERIDAS:\n"
                      + $"1. Deshabilitar inmediatamente el usuario SAP {uSAP} y su cuenta Entra ID asociada.\n"
                      + "2. Revisar la bitácora de transacciones ejecutadas en los últimos 90 días.\n"
                      + "3. Notificar formalmente a RRHH, Contraloría y al área propietaria del sistema.\n"
                      + "4. Implementar integración automática entre sistema de nómina y SAP para revocación al cese.\n"
                      + "5. Documentar la brecha y la acción correctiva en el registro de incidentes de seguridad.\n\n"
                      + $"RESPONSABLE SUGERIDO: Administrador de Seguridad TI / RRHH\n"
                      + $"PLAZO: {plazo}"
            ),

            "CONFLICTO_SOD" => (
                norma:  "COBIT 2019 DSS05.04 / COSO Principio 10 (Controles de Actividad) / SOX Sección 404",
                riesgo: "Capacidad de un único usuario para ejecutar transacciones conflictivas que eliminan "
                      + "la barrera de control dual. Riesgo de fraude, omisión de errores y manipulación "
                      + "no detectada de transacciones financieras o de inventario.",
                plan:   $"OBSERVACIÓN: Usuario {uSAP} tiene asignado simultáneamente roles que crean un conflicto de Segregación de Funciones (SoD): {rol}.\n\n"
                      + "CAUSA RAÍZ: Falta de revisión periódica de la matriz de conflictos SoD al momento de asignar roles. "
                      + "Posible ausencia de control preventivo en el proceso de gestión de accesos.\n\n"
                      + "ACCIONES REQUERIDAS:\n"
                      + $"1. Analizar las transacciones ejecutadas por {uSAP} bajo los roles en conflicto (últimos 90 días).\n"
                      + "2. Verificar si existe justificación de negocio documentada y vigente en SE Suite.\n"
                      + "3. Si NO hay justificación: remover el rol de mayor riesgo de la asignación inmediatamente.\n"
                      + "4. Si HAY justificación: documentar controles compensatorios formales (supervisión, revisión periódica, doble firma).\n"
                      + "5. Revisar y actualizar la matriz de conflictos SoD para todos los usuarios del sistema.\n"
                      + "6. Implementar alerta preventiva en el proceso de asignación de roles.\n\n"
                      + $"RESPONSABLE SUGERIDO: Administrador SAP / Contraloría\n"
                      + $"PLAZO: {plazo}"
            ),

            "ROL_NO_AUTORIZADO_MATRIZ" => (
                norma:  "COBIT 2019 APO01.02 / ISO 27001:2022 A.9.2.2 / COSO Principio 10",
                riesgo: "Usuario con accesos que exceden los definidos para su puesto en la Matriz de Puestos "
                      + "aprobada por Contraloría. Riesgo de acceso a funciones sensibles sin justificación "
                      + "formal y sin control compensatorio documentado.",
                plan:   $"OBSERVACIÓN: El usuario {uSAP} tiene asignado el rol '{rol}' que no está autorizado "
                      + "para su puesto en la Matriz de Puestos SAP aprobada por Contraloría.\n\n"
                      + "CAUSA RAÍZ: El proceso de asignación de roles no valida en tiempo real contra la Matriz de Puestos. "
                      + "Posible asignación manual sin aprobación formal o reasignación de puesto sin revisión de accesos.\n\n"
                      + "ACCIONES REQUERIDAS:\n"
                      + $"1. Verificar si el acceso tiene justificación formal vigente en SE Suite.\n"
                      + "2. Si NO existe caso SE Suite: solicitar justificación al responsable del área en un plazo máximo de 48h.\n"
                      + "3. Si no se justifica: remover el rol no autorizado del perfil de {uSAP}.\n"
                      + "4. Si se justifica: abrir caso en SE Suite con aprobación de Contraloría y fecha de vencimiento.\n"
                      + "5. Evaluar si la Matriz de Puestos requiere actualización para reflejar el cambio.\n\n"
                      + $"RESPONSABLE SUGERIDO: Propietario del área / Administrador SAP / Contraloría\n"
                      + $"PLAZO: {plazo}"
            ),

            "CASO_SESUITE_VENCIDO" => (
                norma:  "COBIT 2019 APO01.03 / ISO 27001:2022 A.9.2.5 / Política de Gestión de Excepciones",
                riesgo: "Acceso temporal otorgado por excepción continúa activo sin renovación de la aprobación. "
                      + "El control de accesos basado en casos de excepción pierde efectividad cuando los casos vencen "
                      + "sin revisión, perpetuando accesos no autorizados.",
                plan:   $"OBSERVACIÓN: El usuario {uSAP} mantiene acceso al rol '{rol}' amparado en un caso SE Suite vencido.\n\n"
                      + "CAUSA RAÍZ: Ausencia de proceso automatizado de revisión y vencimiento de casos de excepción. "
                      + "El sistema no revoca accesos al vencer el caso en SE Suite.\n\n"
                      + "ACCIONES REQUERIDAS:\n"
                      + "1. Notificar al responsable del área sobre el vencimiento del caso SE Suite.\n"
                      + "2. Determinar si el acceso temporal aún es necesario para las funciones del puesto.\n"
                      + "3. Si sigue siendo necesario: renovar el caso en SE Suite con nueva aprobación de Contraloría.\n"
                      + "4. Si ya no es necesario: remover el rol del perfil de usuario inmediatamente.\n"
                      + "5. Implementar alerta automática 30 días antes del vencimiento de casos de excepción.\n\n"
                      + $"RESPONSABLE SUGERIDO: Propietario del caso / Administrador SAP\n"
                      + $"PLAZO: {plazo}"
            ),

            "EMPLEADO_SIN_CUENTA_SAP" => (
                norma:  "COBIT 2019 APO01.02 / ISO 27001:2022 A.9.2.1 / Política de Aprovisionamiento de Accesos",
                riesgo: "Empleado activo con posible uso de cuentas genéricas o de terceros para acceder a SAP, "
                      + "lo que imposibilita la trazabilidad de operaciones y viola el principio de rendición de cuentas.",
                plan:   "OBSERVACIÓN: Empleado activo en nómina sin cuenta SAP nominal asignada.\n\n"
                      + "CAUSA RAÍZ: Posible fallo en el proceso de onboarding o demora en la creación de la cuenta. "
                      + "Puede estar usando una cuenta genérica o de otro usuario.\n\n"
                      + "ACCIONES REQUERIDAS:\n"
                      + "1. Verificar con el área responsable si el empleado requiere acceso a SAP según sus funciones.\n"
                      + "2. Revisar si el empleado está accediendo con credenciales de otro usuario (cuenta compartida).\n"
                      + "3. Si requiere acceso: solicitar creación de cuenta nominal al área de TI.\n"
                      + "4. Asignar roles según la Matriz de Puestos correspondiente.\n"
                      + "5. Revisar y mejorar el proceso de onboarding para garantizar aprovisionamiento oportuno.\n\n"
                      + "RESPONSABLE SUGERIDO: RRHH / Administrador SAP\n"
                      + $"PLAZO: {plazo}"
            ),

            "SAP_ACTIVO_ENTRA_ID_DESHABILITADO" => (
                norma:  "ISO 27001:2022 A.9.2.5 / COBIT 2019 DSS05.04 / NIST CSF PR.AC-1",
                riesgo: "Inconsistencia entre el estado de la cuenta SAP y el directorio corporativo Entra ID. "
                      + "El usuario puede acceder a SAP sin que el control de identidad central lo impida, "
                      + "degradando la efectividad del gobierno de identidades.",
                plan:   $"OBSERVACIÓN: El usuario SAP {uSAP} tiene estado ACTIVO mientras su cuenta en Azure AD (Entra ID) está DESHABILITADA.\n\n"
                      + "CAUSA RAÍZ: Falta de sincronización automática entre el directorio Entra ID y el sistema SAP. "
                      + "Los procesos de gestión de identidades operan de forma independiente sin verificación cruzada.\n\n"
                      + "ACCIONES REQUERIDAS:\n"
                      + $"1. Investigar inmediatamente el motivo de la deshabilitación en Entra ID para el usuario {uSAP}.\n"
                      + "2. Si fue deshabilitado por salida o sanción: deshabilitar el usuario SAP de forma inmediata.\n"
                      + "3. Revisar transacciones ejecutadas en SAP después de la fecha de deshabilitación en AD.\n"
                      + "4. Implementar sincronización automática entre Entra ID y SAP (SCIM o SAP Identity Management).\n"
                      + "5. Establecer revisión periódica (mínimo mensual) de coherencia entre ambos sistemas.\n\n"
                      + "RESPONSABLE SUGERIDO: Administrador de Seguridad TI / Equipo SAP Basis\n"
                      + $"PLAZO: {plazo}"
            ),

            "EX_EMPLEADO_ENTRA_ID_ACTIVO" => (
                norma:  "ISO 27001:2022 A.5.18 / COBIT 2019 APO01.03 / NIST CSF PR.AC-4",
                riesgo: "Ex-empleado con cuenta de directorio habilitada tiene acceso potencial a Microsoft 365, "
                      + "SharePoint, Teams, correo corporativo y todas las aplicaciones integradas con Entra ID. "
                      + "Riesgo elevado de exfiltración de información y acceso no autorizado.",
                plan:   "OBSERVACIÓN: Ex-empleado con estado laboral BAJA o INACTIVO mantiene cuenta Entra ID habilitada.\n\n"
                      + "CAUSA RAÍZ: El proceso de offboarding no incluye la deshabilitación oportuna de la cuenta en Azure AD. "
                      + "Falta de integración entre el sistema de RRHH y el directorio corporativo.\n\n"
                      + "ACCIONES REQUERIDAS:\n"
                      + "1. Deshabilitar la cuenta en Entra ID de forma inmediata.\n"
                      + "2. Revocar todos los tokens de acceso activos para invalidar sesiones vigentes.\n"
                      + "3. Revisar la actividad reciente en M365, SharePoint, Teams y aplicaciones integradas.\n"
                      + "4. Verificar si el ex-empleado tiene accesos adicionales en SAP, ERP u otros sistemas.\n"
                      + "5. Auditar el proceso de offboarding con RRHH para incluir deshabilitación automática de AD.\n"
                      + "6. Documentar el incidente y reportar si se detectó actividad posterior a la fecha de baja.\n\n"
                      + "RESPONSABLE SUGERIDO: Administrador Entra ID / RRHH\n"
                      + $"PLAZO: {plazo}"
            ),

            "EMPLEADO_SIN_CUENTA_ENTRA_ID" => (
                norma:  "ISO 27001:2022 A.9.2.1 / COBIT 2019 APO01.02 / Política de Aprovisionamiento",
                riesgo: "Empleado activo sin identidad digital en el directorio corporativo. Imposibilita la "
                      + "autenticación SSO, el control de accesos centralizado y la trazabilidad de actividad "
                      + "en aplicaciones integradas con Entra ID.",
                plan:   "OBSERVACIÓN: Empleado activo en nómina sin cuenta en Entra ID o con EmployeeId no coincidente con cédula.\n\n"
                      + "CAUSA RAÍZ: Posible fallo en el proceso de onboarding, error en el campo EmployeeId de Azure AD, "
                      + "o empleado sin cuenta digital aprovisionada.\n\n"
                      + "ACCIONES REQUERIDAS:\n"
                      + "1. Verificar en Azure AD si existe una cuenta con el nombre del empleado y el EmployeeId correcto.\n"
                      + "2. Si el EmployeeId está vacío o incorrecto: actualizar el campo en Azure AD con la cédula.\n"
                      + "3. Si no existe cuenta: solicitar aprovisionamiento al área de TI a través del proceso de onboarding.\n"
                      + "4. Revisar el proceso de onboarding para garantizar el aprovisionamiento automático con cédula como EmployeeId.\n\n"
                      + "RESPONSABLE SUGERIDO: Administrador Entra ID / RRHH\n"
                      + $"PLAZO: {plazo}"
            ),

            _ => (
                norma:  "ISO 27001:2022 / COBIT 2019 / COSO",
                riesgo: "Hallazgo de control identificado en la revisión de accesos del sistema.",
                plan:   "Revisar el hallazgo, identificar la causa raíz y definir acciones correctivas con el área responsable.\n\n"
                      + $"PLAZO SUGERIDO: {plazo}"
            )
        };
    }
}
