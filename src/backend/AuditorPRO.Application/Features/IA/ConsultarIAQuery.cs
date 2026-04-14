using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System.Text;

namespace AuditorPRO.Application.Features.IA;

public record ConsultarIAQuery(string Pregunta, string? ContextoAdicional = null) : IRequest<IAResponseDto>;

public class ConsultarIAValidator : AbstractValidator<ConsultarIAQuery>
{
    public ConsultarIAValidator()
    {
        RuleFor(x => x.Pregunta).NotEmpty().MaximumLength(2000);
    }
}

public class IAResponseDto
{
    public string Respuesta { get; set; } = string.Empty;
    public string? FuentesConsultadas { get; set; }
    public bool UsoBaseConocimiento { get; set; }
    public bool UsoContextoSAP { get; set; }
    public DateTime GeneradoAt { get; set; } = DateTime.UtcNow;
}

public class ConsultarIAHandler : IRequestHandler<ConsultarIAQuery, IAResponseDto>
{
    private readonly IAzureOpenAIService _iaService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLoggerService _auditLogger;
    private readonly IIngestorDocumentosService _ingestor;
    private readonly IRepository<UsuarioSistema> _usuarioRepo;
    private readonly IRepository<RolSistema> _rolRepo;
    private readonly IRepository<AsignacionRolUsuario> _asignRepo;
    private readonly IRepository<EmpleadoMaestro> _empleadoRepo;
    private readonly IRepository<SnapshotEntraID> _snapshotRepo;
    private readonly IRepository<RegistroEntraID> _registroEntraRepo;
    private readonly IRepository<Hallazgo> _hallazgoRepo;
    private readonly IRepository<MatrizPuestoSAP> _matrizRepo;

    private const string ContextoOrganizacional = """
        Eres el Agente Auditor Preventivo de AuditorPRO TI para ILG Logistics.
        Tu rol es asistir a auditores internos y administradores de TI a identificar debilidades de control,
        evaluar riesgos de segregación de funciones, revisar políticas de seguridad y sugerir planes de acción.
        Baséate en marcos normativos: ISO 27001, COBIT 2019, COSO, SOX, NIST CSF.
        Responde siempre en español, con base en evidencia y fundamento normativo.
        Cuando tengas datos reales de la base de datos, úsalos como evidencia concreta.
        Cuando tengas documentos de la base de conocimiento, cítalos explícitamente.
        """;

    public ConsultarIAHandler(
        IAzureOpenAIService iaService,
        ICurrentUserService currentUser,
        IAuditLoggerService auditLogger,
        IIngestorDocumentosService ingestor,
        IRepository<UsuarioSistema> usuarioRepo,
        IRepository<RolSistema> rolRepo,
        IRepository<AsignacionRolUsuario> asignRepo,
        IRepository<EmpleadoMaestro> empleadoRepo,
        IRepository<SnapshotEntraID> snapshotRepo,
        IRepository<RegistroEntraID> registroEntraRepo,
        IRepository<Hallazgo> hallazgoRepo,
        IRepository<MatrizPuestoSAP> matrizRepo)
    {
        _iaService = iaService; _currentUser = currentUser;
        _auditLogger = auditLogger; _ingestor = ingestor;
        _usuarioRepo = usuarioRepo; _rolRepo = rolRepo;
        _asignRepo = asignRepo; _empleadoRepo = empleadoRepo;
        _snapshotRepo = snapshotRepo; _registroEntraRepo = registroEntraRepo;
        _hallazgoRepo = hallazgoRepo; _matrizRepo = matrizRepo;
    }

    public async Task<IAResponseDto> Handle(ConsultarIAQuery request, CancellationToken ct)
    {
        // 1. RAG — documentos de la base de conocimiento
        var docsRelevantes = await _ingestor.BuscarAsync(request.Pregunta, topK: 4, ct);
        var fuentes = new List<string>();
        var sbRAG = new StringBuilder();

        if (docsRelevantes.Count > 0)
        {
            sbRAG.AppendLine("\n=== DOCUMENTOS DE AUDITORÍA (Base de Conocimiento) ===");
            foreach (var doc in docsRelevantes)
            {
                var fragmento = doc.TextoCompleto.Length > 800 ? doc.TextoCompleto[..800] + "..." : doc.TextoCompleto;
                sbRAG.AppendLine($"\n[Fuente: {doc.NombreArchivo} | Dominio: {doc.DominioDetectado ?? "General"}]");
                sbRAG.AppendLine(fragmento);
                fuentes.Add(doc.NombreArchivo);
            }
            sbRAG.AppendLine("=== FIN DOCUMENTOS ===\n");
        }

        // 2. Contexto SAP en tiempo real — consulta BD según intent detectado
        var (contextoSAP, usoSAP) = await ConstruirContextoSAPAsync(request.Pregunta, ct);

        // 3. Construir prompt final
        var contextoCompleto = new StringBuilder(ContextoOrganizacional);
        if (sbRAG.Length > 0) contextoCompleto.Append(sbRAG);
        if (usoSAP)          contextoCompleto.Append(contextoSAP);
        if (!string.IsNullOrWhiteSpace(request.ContextoAdicional))
            contextoCompleto.Append($"\n\nContexto adicional:\n{request.ContextoAdicional}");

        // 4. Llamar al modelo
        var respuesta = await _iaService.ConsultarAsync(request.Pregunta, contextoCompleto.ToString(), ct);

        await _auditLogger.LogAsync(_currentUser.UserId, _currentUser.Email,
            "CONSULTA_IA", "AgentIA", null, ct: ct);

        return new IAResponseDto
        {
            Respuesta          = respuesta,
            FuentesConsultadas = fuentes.Count > 0 ? string.Join(", ", fuentes) : null,
            UsoBaseConocimiento = docsRelevantes.Count > 0,
            UsoContextoSAP     = usoSAP,
        };
    }

    // ─── Constructor de contexto SAP dinámico ─────────────────────────────────
    private async Task<(string contexto, bool usado)> ConstruirContextoSAPAsync(string pregunta, CancellationToken ct)
    {
        var q = pregunta.ToUpperInvariant();
        var sb = new StringBuilder();
        bool usado = false;

        // ── Detectar menciones de usuario SAP específico ──────────────────────
        var usuarios = (await _usuarioRepo.FindAsync(u => u.Sistema == "SAP", ct)).ToList();
        var usuarioMencionado = usuarios.FirstOrDefault(u =>
            q.Contains(u.NombreUsuario.ToUpperInvariant()) ||
            (!string.IsNullOrWhiteSpace(u.Cedula) && q.Contains(u.Cedula)) ||
            (!string.IsNullOrWhiteSpace(u.NombreCompleto) && q.Contains(u.NombreCompleto.ToUpperInvariant())));

        if (usuarioMencionado != null)
        {
            usado = true;
            sb.AppendLine("\n=== DATOS SAP — USUARIO ESPECÍFICO ===");
            sb.AppendLine($"Usuario: {usuarioMencionado.NombreUsuario}");
            sb.AppendLine($"Nombre:  {usuarioMencionado.NombreCompleto}");
            sb.AppendLine($"Cédula:  {usuarioMencionado.Cedula ?? "—"}");
            sb.AppendLine($"Puesto:  {usuarioMencionado.Puesto}");
            sb.AppendLine($"Depto:   {usuarioMencionado.Departamento}");
            sb.AppendLine($"Estado:  {usuarioMencionado.Estado}");

            // Roles asignados
            var roles    = (await _rolRepo.FindAsync(r => r.Sistema == "SAP", ct)).ToDictionary(r => r.Id);
            var asigns   = (await _asignRepo.FindAsync(a => a.UsuarioId == usuarioMencionado.Id && a.Activa, ct)).ToList();
            sb.AppendLine($"\nRoles asignados ({asigns.Count}):");
            foreach (var a in asigns)
            {
                if (roles.TryGetValue(a.RolId, out var rol))
                {
                    var tcodes = string.IsNullOrWhiteSpace(rol.TransaccionesAutorizadas) ? "—"
                        : rol.TransaccionesAutorizadas.Length > 120
                            ? rol.TransaccionesAutorizadas[..120] + "..."
                            : rol.TransaccionesAutorizadas;
                    sb.AppendLine($"  • {rol.NombreRol}{(rol.EsCritico ? " [CRÍTICO]" : "")} → Transacciones: {tcodes}");
                }
            }

            // Cruce con Entra ID
            if (!string.IsNullOrWhiteSpace(usuarioMencionado.Cedula))
            {
                var latestSnap = (await _snapshotRepo.GetAllAsync(ct))
                    .OrderByDescending(s => s.FechaInstantanea).FirstOrDefault();
                if (latestSnap != null)
                {
                    var regEntra = (await _registroEntraRepo.FindAsync(
                        r => r.SnapshotId == latestSnap.Id && r.EmployeeId == usuarioMencionado.Cedula, ct))
                        .FirstOrDefault();
                    if (regEntra != null)
                        sb.AppendLine($"\nEntra ID (snapshot {latestSnap.FechaInstantanea:dd/MM/yyyy}): " +
                            $"UPN={regEntra.UserPrincipalName}, AccountEnabled={regEntra.AccountEnabled}, Dept={regEntra.Department}");
                    else
                        sb.AppendLine($"\nEntra ID: no encontrado en snapshot del {latestSnap.FechaInstantanea:dd/MM/yyyy}");
                }
            }

            // Cruce con Matriz de Puestos (por cédula o usuario SAP)
            var matrizUsuario = (await _matrizRepo.FindAsync(m =>
                (!string.IsNullOrWhiteSpace(usuarioMencionado.Cedula) && m.Cedula == usuarioMencionado.Cedula) ||
                m.UsuarioSAP == usuarioMencionado.NombreUsuario, ct)).ToList();

            if (matrizUsuario.Count > 0)
            {
                var fechaRev = matrizUsuario.FirstOrDefault(m => m.FechaRevisionContraloria.HasValue)?.FechaRevisionContraloria;
                sb.AppendLine($"\nMatriz de Puestos (revisión Contraloría: {(fechaRev.HasValue ? fechaRev.Value.ToString("dd/MM/yyyy") : "—")}):");
                sb.AppendLine($"  Puesto autorizado: {matrizUsuario.First().Puesto}");
                sb.AppendLine($"  Roles autorizados en Matriz ({matrizUsuario.Count}):");
                foreach (var m in matrizUsuario.Take(30))
                {
                    var trans = string.IsNullOrWhiteSpace(m.Transaccion) ? "—" : m.Transaccion;
                    sb.AppendLine($"    ✓ {m.Rol} | Validez: {m.InicioValidez?.ToString("dd/MM/yy") ?? "—"} → {m.FinValidez?.ToString("dd/MM/yy") ?? "∞"} | T-code: {trans}");
                }
                if (matrizUsuario.Count > 30) sb.AppendLine($"    ... y {matrizUsuario.Count - 30} más en Matriz");

                // Detectar roles SAP que NO están en la Matriz
                var rolesAsignNombres = asigns
                    .Where(a => roles.TryGetValue(a.RolId, out _))
                    .Select(a => roles[a.RolId].NombreRol)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var rolesAutorizados = matrizUsuario.Select(m => m.Rol).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var rolesExcedentes = rolesAsignNombres.Where(r => !rolesAutorizados.Contains(r)).ToList();
                var rolesFaltantes  = rolesAutorizados.Where(r => !rolesAsignNombres.Contains(r)).ToList();

                if (rolesExcedentes.Count > 0)
                    sb.AppendLine($"\n  ⚠ ROLES EN SAP QUE EXCEDEN LA MATRIZ ({rolesExcedentes.Count}): {string.Join(", ", rolesExcedentes.Take(10))}");
                if (rolesFaltantes.Count > 0)
                    sb.AppendLine($"  ℹ Roles en Matriz no asignados en SAP ({rolesFaltantes.Count}): {string.Join(", ", rolesFaltantes.Take(10))}");
                if (rolesExcedentes.Count == 0 && rolesFaltantes.Count == 0)
                    sb.AppendLine("  ✓ Accesos SAP alineados con la Matriz de Puestos aprobada");
            }
            else
            {
                sb.AppendLine($"\nMatriz de Puestos: sin registros para este usuario" +
                    (string.IsNullOrWhiteSpace(usuarioMencionado.Cedula) ? " (sin cédula registrada)" : " — posible hallazgo R03"));
            }

            sb.AppendLine("=== FIN USUARIO ===\n");
        }

        // ── Detectar menciones de rol SAP específico ──────────────────────────
        var roles2 = (await _rolRepo.FindAsync(r => r.Sistema == "SAP", ct)).ToList();
        var rolMencionado = roles2.FirstOrDefault(r => q.Contains(r.NombreRol.ToUpperInvariant()));

        if (rolMencionado != null && usuarioMencionado == null)
        {
            usado = true;
            var asignaciones = (await _asignRepo.FindAsync(a => a.RolId == rolMencionado.Id && a.Activa, ct)).ToList();
            var usuariosConRol = usuarios.Where(u => asignaciones.Any(a => a.UsuarioId == u.Id)).ToList();

            sb.AppendLine($"\n=== DATOS SAP — ROL: {rolMencionado.NombreRol} ===");
            sb.AppendLine($"Crítico: {(rolMencionado.EsCritico ? "SÍ" : "No")} | Riesgo: {rolMencionado.NivelRiesgo ?? "—"}");
            sb.AppendLine($"Transacciones: {rolMencionado.TransaccionesAutorizadas ?? "—"}");
            sb.AppendLine($"\nUsuarios con este rol ({usuariosConRol.Count}):");
            foreach (var u in usuariosConRol.Take(20))
                sb.AppendLine($"  • {u.NombreUsuario} — {u.NombreCompleto} ({u.Puesto}) [{u.Estado}]");
            if (usuariosConRol.Count > 20) sb.AppendLine($"  ... y {usuariosConRol.Count - 20} más");

            // Cruce con Matriz de Puestos — ¿este rol está autorizado para qué puestos?
            var matrizRol = (await _matrizRepo.FindAsync(m =>
                m.Rol.ToUpper() == rolMencionado.NombreRol.ToUpper(), ct)).ToList();
            if (matrizRol.Count > 0)
            {
                var puestosAutorizados = matrizRol.Select(m => m.Puesto).Distinct().ToList();
                var fechaRev = matrizRol.FirstOrDefault(m => m.FechaRevisionContraloria.HasValue)?.FechaRevisionContraloria;
                sb.AppendLine($"\nMatriz de Puestos (revisión Contraloría: {(fechaRev.HasValue ? fechaRev.Value.ToString("dd/MM/yyyy") : "—")}):");
                sb.AppendLine($"  Puestos autorizados para este rol ({puestosAutorizados.Count}): {string.Join(", ", puestosAutorizados)}");

                // Usuarios con este rol cuyo puesto NO está en la Matriz para este rol
                var puestosSet = puestosAutorizados.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var usuariosNoAutorizados = usuariosConRol.Where(u => !string.IsNullOrWhiteSpace(u.Puesto) && !puestosSet.Contains(u.Puesto)).ToList();
                if (usuariosNoAutorizados.Count > 0)
                    sb.AppendLine($"  ⚠ Usuarios con puesto NO autorizado en Matriz ({usuariosNoAutorizados.Count}): " +
                        string.Join(", ", usuariosNoAutorizados.Take(10).Select(u => $"{u.NombreUsuario}({u.Puesto})")));
                else
                    sb.AppendLine($"  ✓ Todos los usuarios con este rol tienen puesto autorizado en la Matriz");
            }
            else
            {
                sb.AppendLine($"\nMatriz de Puestos: este rol no aparece en la Matriz aprobada por Contraloría — hallazgo potencial");
            }
            sb.AppendLine("=== FIN ROL ===\n");
        }

        // ── Detectar mención de transacción SAP ───────────────────────────────
        var palabras = pregunta.Split([' ', ',', '?', '.', ';'], StringSplitOptions.RemoveEmptyEntries);
        var tcodeMencionado = palabras.FirstOrDefault(p =>
            p.Length >= 2 && p.Length <= 10 && roles2.Any(r =>
                r.TransaccionesAutorizadas != null &&
                r.TransaccionesAutorizadas.ToUpperInvariant().Split(',').Contains(p.ToUpperInvariant())));

        if (!string.IsNullOrWhiteSpace(tcodeMencionado) && usuarioMencionado == null && rolMencionado == null)
        {
            usado = true;
            var tUpper = tcodeMencionado.ToUpperInvariant();
            var rolesConTcode = roles2.Where(r =>
                r.TransaccionesAutorizadas != null &&
                r.TransaccionesAutorizadas.ToUpperInvariant().Split(',').Contains(tUpper)).ToList();

            sb.AppendLine($"\n=== DATOS SAP — TRANSACCIÓN: {tUpper} ===");
            sb.AppendLine($"Roles que incluyen esta transacción ({rolesConTcode.Count}):");
            foreach (var r in rolesConTcode)
            {
                var asignCount = (await _asignRepo.FindAsync(a => a.RolId == r.Id && a.Activa, ct)).Count();
                sb.AppendLine($"  • {r.NombreRol}{(r.EsCritico ? " [CRÍTICO]" : "")} — {asignCount} usuarios");
            }
            sb.AppendLine("=== FIN TRANSACCIÓN ===\n");
        }

        // ── Consulta general de accesos / resumen ─────────────────────────────
        bool esConsultaGeneral =
            q.ContainsAny("CUÁNTOS", "CUANTOS", "RESUMEN", "TOTAL", "ESTADÍSTICA",
                          "ESTADISTICA", "ACCESOS", "USUARIOS SAP", "ROLES SAP",
                          "QUIÉN TIENE", "QUIEN TIENE", "LISTADO", "TODOS LOS");

        if (esConsultaGeneral && usuarioMencionado == null && rolMencionado == null)
        {
            usado = true;
            var totalAsign  = (await _asignRepo.GetAllAsync(ct)).Count();
            var rolesCritic = roles2.Count(r => r.EsCritico);
            var usrActivos  = usuarios.Count(u => u.Estado == EstadoUsuario.ACTIVO);

            sb.AppendLine("\n=== RESUMEN DE ACCESOS SAP ===");
            sb.AppendLine($"Usuarios SAP totales:    {usuarios.Count} ({usrActivos} activos)");
            sb.AppendLine($"Roles únicos:            {roles2.Count} ({rolesCritic} críticos)");
            sb.AppendLine($"Asignaciones usuario+rol:{totalAsign}");

            // Top 10 usuarios con más roles
            var asignTodas = (await _asignRepo.FindAsync(a => a.Activa, ct)).ToList();
            var topUsuarios = asignTodas
                .GroupBy(a => a.UsuarioId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => (usr: usuarios.FirstOrDefault(u => u.Id == g.Key), count: g.Count()))
                .Where(x => x.usr != null);

            sb.AppendLine("\nTop 10 usuarios con más roles:");
            foreach (var (usr, count) in topUsuarios)
                sb.AppendLine($"  • {usr!.NombreUsuario} — {usr.NombreCompleto} — {count} roles");

            // Snapshot Entra ID
            var snap = (await _snapshotRepo.GetAllAsync(ct)).OrderByDescending(s => s.FechaInstantanea).FirstOrDefault();
            if (snap != null)
                sb.AppendLine($"\nÚltimo snapshot Entra ID: {snap.Nombre} ({snap.FechaInstantanea:dd/MM/yyyy}) — {snap.TotalRegistros} usuarios");

            sb.AppendLine("=== FIN RESUMEN ===\n");
        }

        // ── Consulta sobre Matriz de Puestos ─────────────────────────────────
        bool esConsultaMatriz =
            q.ContainsAny("MATRIZ", "PUESTOS", "CONTRALORIA", "CONTRALORÍA",
                          "AUTORIZADO", "AUTORIZADA", "APROBADO", "APROBADA",
                          "EXCEDE", "EXCESO", "SEGREGACION", "SEGREGACIÓN");

        if (esConsultaMatriz && usuarioMencionado == null && rolMencionado == null)
        {
            usado = true;
            var todaMatriz = (await _matrizRepo.GetAllAsync(ct)).ToList();
            if (todaMatriz.Count > 0)
            {
                var puestosDistintos = todaMatriz.Select(m => m.Puesto).Distinct().Count();
                var rolesDistintos   = todaMatriz.Select(m => m.Rol).Distinct().Count();
                var fechaRev = todaMatriz
                    .Where(m => m.FechaRevisionContraloria.HasValue)
                    .OrderByDescending(m => m.FechaRevisionContraloria)
                    .FirstOrDefault()?.FechaRevisionContraloria;

                sb.AppendLine("\n=== MATRIZ DE PUESTOS SAP (Aprobada por Contraloría) ===");
                sb.AppendLine($"Total de registros:          {todaMatriz.Count}");
                sb.AppendLine($"Puestos distintos cubiertos: {puestosDistintos}");
                sb.AppendLine($"Roles distintos autorizados: {rolesDistintos}");
                sb.AppendLine($"Fecha revisión Contraloría:  {(fechaRev.HasValue ? fechaRev.Value.ToString("dd/MM/yyyy") : "—")}");

                // Top puestos con más roles autorizados
                var topPuestos = todaMatriz
                    .GroupBy(m => m.Puesto)
                    .OrderByDescending(g => g.Count())
                    .Take(10);
                sb.AppendLine("\nTop puestos con más roles autorizados:");
                foreach (var g in topPuestos)
                    sb.AppendLine($"  • {g.Key}: {g.Count()} roles");

                // Cruce global: usuarios SAP cuyos roles exceden la Matriz
                var allUsuarios2 = (await _usuarioRepo.FindAsync(u => u.Sistema == "SAP", ct)).ToList();
                var allAsigns2   = (await _asignRepo.FindAsync(a => a.Activa, ct)).ToList();
                var allRoles2    = (await _rolRepo.FindAsync(r => r.Sistema == "SAP", ct)).ToDictionary(r => r.Id);
                var matrizPorUsuSap = todaMatriz.GroupBy(m => m.UsuarioSAP, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Select(m => m.Rol).ToHashSet(StringComparer.OrdinalIgnoreCase));

                int usuariosConExceso = 0;
                foreach (var uSAP in allUsuarios2)
                {
                    if (!matrizPorUsuSap.TryGetValue(uSAP.NombreUsuario, out var rolesAutoriz)) continue;
                    var rolesActuales = allAsigns2
                        .Where(a => a.UsuarioId == uSAP.Id && allRoles2.ContainsKey(a.RolId))
                        .Select(a => allRoles2[a.RolId].NombreRol)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (rolesActuales.Any(r => !rolesAutoriz.Contains(r))) usuariosConExceso++;
                }
                sb.AppendLine($"\nUsuarios SAP con roles que exceden la Matriz: {usuariosConExceso}");
                sb.AppendLine("=== FIN MATRIZ ===\n");
            }
            else
            {
                sb.AppendLine("\n=== MATRIZ DE PUESTOS SAP ===");
                sb.AppendLine("La Matriz de Puestos aún no ha sido cargada. Sin este archivo no es posible evaluar la regla R03 de Control Cruzado.");
                sb.AppendLine("=== FIN MATRIZ ===\n");
            }
        }

        // ── Consulta sobre hallazgos ───────────────────────────────────────────
        bool esConsultaHallazgos =
            q.ContainsAny("HALLAZGO", "HALLAZGOS", "RIESGO", "CRITICO", "CRÍTICO",
                          "ABIERTO", "INCUMPLIMIENTO", "CONTROL CRUZADO");

        if (esConsultaHallazgos)
        {
            usado = true;
            var hallazgos = (await _hallazgoRepo.GetAllAsync(ct))
                .OrderByDescending(h => h.Criticidad).Take(15).ToList();
            if (hallazgos.Count > 0)
            {
                sb.AppendLine("\n=== HALLAZGOS RECIENTES ===");
                foreach (var h in hallazgos)
                    sb.AppendLine($"  [{h.Criticidad}] {h.Titulo} — Estado: {h.Estado} | Cédula: {h.Cedula ?? "—"}");
                sb.AppendLine("=== FIN HALLAZGOS ===\n");
                usado = true;
            }
        }

        return (sb.ToString(), usado);
    }
}

// Extensión helper para Contains múltiple
file static class StringExtensions
{
    public static bool ContainsAny(this string source, params string[] values)
        => values.Any(v => source.Contains(v, StringComparison.OrdinalIgnoreCase));
}
