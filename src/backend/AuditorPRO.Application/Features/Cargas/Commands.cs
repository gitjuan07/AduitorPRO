using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using MediatR;
using System.Globalization;
using System.Text;

namespace AuditorPRO.Application.Features.Cargas;

public record CargarEmpleadosCommand(
    Stream Contenido,
    string NombreArchivo,
    string ContentType,
    int SociedadId
) : IRequest<CargaResultado>;

public class CargaResultado
{
    public int TotalRegistros { get; set; }
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public int Errores { get; set; }
    public List<string> DetalleErrores { get; set; } = [];
}

public class CargarEmpleadosValidator : AbstractValidator<CargarEmpleadosCommand>
{
    private static readonly string[] AllowedTypes =
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
         "application/vnd.ms-excel", "text/csv", "application/csv"];

    public CargarEmpleadosValidator()
    {
        RuleFor(x => x.NombreArchivo).NotEmpty();
        RuleFor(x => x.ContentType).Must(t => AllowedTypes.Contains(t))
            .WithMessage("Solo se aceptan archivos Excel (.xlsx) o CSV.");
        RuleFor(x => x.SociedadId).GreaterThan(0);
    }
}

public class CargarEmpleadosHandler : IRequestHandler<CargarEmpleadosCommand, CargaResultado>
{
    private readonly IRepository<EmpleadoMaestro> _repo;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;

    public CargarEmpleadosHandler(IRepository<EmpleadoMaestro> repo, ICurrentUserService user, IAuditLoggerService audit)
    { _repo = repo; _user = user; _audit = audit; }

    public async Task<CargaResultado> Handle(CargarEmpleadosCommand request, CancellationToken ct)
    {
        var filas = request.ContentType.Contains("csv")
            ? ParseCsv(request.Contenido)
            : ParseExcel(request.Contenido);

        var resultado = new CargaResultado { TotalRegistros = filas.Count };
        var existentes = (await _repo.GetAllAsync(ct)).ToDictionary(e => e.NumeroEmpleado);

        foreach (var (fila, idx) in filas.Select((f, i) => (f, i + 2)))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fila.NumeroEmpleado))
                {
                    resultado.Errores++;
                    resultado.DetalleErrores.Add($"Fila {idx}: NumeroEmpleado vacío.");
                    continue;
                }

                if (existentes.TryGetValue(fila.NumeroEmpleado, out var emp))
                {
                    emp.NombreCompleto = fila.NombreCompleto;
                    emp.CorreoCorporativo = fila.Email;
                    emp.EstadoLaboral = fila.Activo ? EstadoLaboral.ACTIVO : EstadoLaboral.BAJA_PROCESADA;
                    emp.UpdatedAt = DateTime.UtcNow;
                    await _repo.UpdateAsync(emp, ct);
                    resultado.Actualizados++;
                }
                else
                {
                    var nuevo = new EmpleadoMaestro
                    {
                        NumeroEmpleado = fila.NumeroEmpleado,
                        NombreCompleto = fila.NombreCompleto,
                        CorreoCorporativo = fila.Email,
                        SociedadId = request.SociedadId,
                        EstadoLaboral = fila.Activo ? EstadoLaboral.ACTIVO : EstadoLaboral.BAJA_PROCESADA,
                        FechaIngreso = fila.FechaIngreso.HasValue ? DateOnly.FromDateTime(fila.FechaIngreso.Value) : DateOnly.FromDateTime(DateTime.UtcNow),
                        CreatedBy = _user.Email
                    };
                    await _repo.AddAsync(nuevo, ct);
                    resultado.Insertados++;
                }
            }
            catch (Exception ex)
            {
                resultado.Errores++;
                resultado.DetalleErrores.Add($"Fila {idx}: {ex.Message}");
            }
        }

        try
        {
            await _repo.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            resultado.Errores += resultado.Insertados + resultado.Actualizados;
            resultado.Insertados = 0;
            resultado.Actualizados = 0;
            resultado.DetalleErrores.Insert(0, $"Error al guardar: {ex.InnerException?.Message ?? ex.Message}");
            return resultado;
        }

        await _audit.LogAsync(_user.UserId, _user.Email, "CARGA_EMPLEADOS", "EmpleadoMaestro",
            null, datosDespues: new { resultado.Insertados, resultado.Actualizados, resultado.Errores }, ct: ct);

        return resultado;
    }

    private static List<FilaEmpleado> ParseExcel(Stream stream)
    {
        // Columnas de la plantilla (ver CargasController.PlantillaEmpleados):
        // 1=NumeroEmpleado, 2=Nombre, 3=ApellidoPaterno, 4=ApellidoMaterno,
        // 5=CorreoCorporativo, 6=FechaIngreso, 7=EstadoLaboral, 8=DepartamentoCodigo, 9=PuestoCodigo
        var filas = new List<FilaEmpleado>();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            var numero = ws.Cell(r, 1).GetString().Trim();
            // Saltar filas vacías o de notas (ej: "* EstadoLaboral: ...")
            if (string.IsNullOrWhiteSpace(numero) || numero.StartsWith('*')) continue;

            var nombre = ws.Cell(r, 2).GetString().Trim();
            var apellidoP = ws.Cell(r, 3).GetString().Trim();
            var apellidoM = ws.Cell(r, 4).GetString().Trim();
            var nombreCompleto = string.Join(" ", new[] { nombre, apellidoP, apellidoM }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            filas.Add(new FilaEmpleado
            {
                NumeroEmpleado = numero,
                NombreCompleto = nombreCompleto,
                Email = ws.Cell(r, 5).GetString().Trim(),
                Activo = ws.Cell(r, 7).GetString().Trim().Equals("ACTIVO", StringComparison.OrdinalIgnoreCase),
                FechaIngreso = ws.Cell(r, 6).TryGetValue<DateTime>(out var d) ? d : null,
                Puesto = ws.Cell(r, 9).GetString().Trim()
            });
        }
        return filas;
    }

    private static List<FilaEmpleado> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<FilaEmpleado>().ToList();
    }

    private class FilaEmpleado
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime? FechaIngreso { get; set; }
    }
}

// ─── Carga de Usuarios SAP / Sistema ────────────────────────────────────────

public record CargarUsuariosSistemaCommand(
    Stream Contenido,
    string NombreArchivo,
    string ContentType,
    string Sistema
) : IRequest<CargaResultado>;

public class CargarUsuariosSistemaHandler : IRequestHandler<CargarUsuariosSistemaCommand, CargaResultado>
{
    private readonly IRepository<UsuarioSistema> _repo;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;

    public CargarUsuariosSistemaHandler(IRepository<UsuarioSistema> repo, ICurrentUserService user, IAuditLoggerService audit)
    { _repo = repo; _user = user; _audit = audit; }

    public async Task<CargaResultado> Handle(CargarUsuariosSistemaCommand request, CancellationToken ct)
    {
        var filas = ParseExcel(request.Contenido, request.Sistema);
        var resultado = new CargaResultado { TotalRegistros = filas.Count };
        var existentes = (await _repo.FindAsync(u => u.Sistema == request.Sistema, ct))
            .ToDictionary(u => u.NombreUsuario);

        foreach (var (fila, idx) in filas.Select((f, i) => (f, i + 2)))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fila.UserId))
                {
                    resultado.Errores++;
                    resultado.DetalleErrores.Add($"Fila {idx}: UserId vacío.");
                    continue;
                }

                if (existentes.TryGetValue(fila.UserId, out var usr))
                {
                    usr.Estado = fila.Activo ? EstadoUsuario.ACTIVO : EstadoUsuario.BLOQUEADO;
                    usr.TipoUsuario = fila.Perfil;
                    usr.UpdatedAt = DateTime.UtcNow;
                    await _repo.UpdateAsync(usr, ct);
                    resultado.Actualizados++;
                }
                else
                {
                    var nuevo = new UsuarioSistema
                    {
                        NombreUsuario = fila.UserId,
                        Sistema = request.Sistema,
                        Estado = fila.Activo ? EstadoUsuario.ACTIVO : EstadoUsuario.BLOQUEADO,
                        TipoUsuario = fila.Perfil,
                        CreatedBy = _user.Email
                    };
                    await _repo.AddAsync(nuevo, ct);
                    resultado.Insertados++;
                }
            }
            catch (Exception ex)
            {
                resultado.Errores++;
                resultado.DetalleErrores.Add($"Fila {idx}: {ex.Message}");
            }
        }

        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(_user.UserId, _user.Email, "CARGA_USUARIOS_SISTEMA", "UsuarioSistema",
            null, datosDespues: new { Sistema = request.Sistema, resultado.Insertados, resultado.Actualizados }, ct: ct);

        return resultado;
    }

    private static List<FilaUsuario> ParseExcel(Stream stream, string sistema)
    {
        var filas = new List<FilaUsuario>();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            filas.Add(new FilaUsuario
            {
                UserId = ws.Cell(r, 1).GetString(),
                NombreCompleto = ws.Cell(r, 2).GetString(),
                Email = ws.Cell(r, 3).GetString(),
                Activo = ws.Cell(r, 4).GetString().Equals("SI", StringComparison.OrdinalIgnoreCase),
                Perfil = ws.Cell(r, 5).GetString()
            });
        }
        return filas;
    }

    private class FilaUsuario
    {
        public string UserId { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string? Perfil { get; set; }
    }
}

// ─── Carga de Usuarios SAP con Roles y Transacciones ────────────────────────

public record CargarRolesSAPCommand(
    Stream Contenido,
    string NombreArchivo,
    string ContentType,
    string Sistema
) : IRequest<CargaResultado>;

public class CargarRolesSAPHandler : IRequestHandler<CargarRolesSAPCommand, CargaResultado>
{
    private readonly IRepository<UsuarioSistema> _usuarioRepo;
    private readonly IRepository<RolSistema> _rolRepo;
    private readonly IRepository<AsignacionRolUsuario> _asignRepo;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;

    public CargarRolesSAPHandler(
        IRepository<UsuarioSistema> usuarioRepo,
        IRepository<RolSistema> rolRepo,
        IRepository<AsignacionRolUsuario> asignRepo,
        ICurrentUserService user,
        IAuditLoggerService audit)
    { _usuarioRepo = usuarioRepo; _rolRepo = rolRepo; _asignRepo = asignRepo; _user = user; _audit = audit; }

    public async Task<CargaResultado> Handle(CargarRolesSAPCommand request, CancellationToken ct)
    {
        var filas = ParseExcel(request.Contenido);
        var resultado = new CargaResultado { TotalRegistros = filas.Count };
        const int BATCH = 500; // filas por SaveChanges — evita acumular 15k entidades en memoria

        // ── Cargar existentes (tracked por EF → cambios detectados automáticamente) ──
        var usuarios = (await _usuarioRepo.FindAsync(u => u.Sistema == request.Sistema, ct))
            .ToDictionary(u => u.NombreUsuario.ToUpper());
        var roles = (await _rolRepo.FindAsync(r => r.Sistema == request.Sistema, ct))
            .ToDictionary(r => r.NombreRol.ToUpper());
        var asignaciones = (await _asignRepo.FindAsync(a => true, ct))
            .GroupBy(a => a.UsuarioId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.RolId).ToHashSet());

        // ── PASADA 1: upsert UsuarioSistema + RolSistema ─────────────────────
        // IMPORTANTE: nunca llamar UpdateAsync sobre entidades en estado "Added" —
        // _dbSet.Update() cambiaría el estado a "Modified" y el INSERT fallaría (0 rows).
        // Solución: solo modificar propiedades directamente; EF change tracker detecta
        // el estado Modified automáticamente para entidades cargadas desde la BD.
        var pendientesAsign = new List<(UsuarioSistema usr, RolSistema rol, FilaRolSAP fila)>();
        int pendientesBatch = 0;

        foreach (var (fila, idx) in filas.Select((f, i) => (f, i + 2)))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fila.UsuarioSAP))
                {
                    resultado.Errores++;
                    resultado.DetalleErrores.Add($"Fila {idx}: UsuarioSAP vacío.");
                    continue;
                }

                // 1. Upsert UsuarioSistema — solo modificar propiedades, sin UpdateAsync
                if (!usuarios.TryGetValue(fila.UsuarioSAP.ToUpper(), out var usr))
                {
                    usr = new UsuarioSistema
                    {
                        Sistema           = request.Sistema,
                        NombreUsuario     = fila.UsuarioSAP.ToUpper(),
                        Cedula            = fila.Cedula,
                        NombreCompleto    = fila.NombreCompleto,
                        Sociedad          = fila.Sociedad,
                        Departamento      = fila.Departamento,
                        Puesto            = fila.Puesto,
                        Email             = fila.Email,
                        Estado            = fila.Estado,
                        TipoUsuario       = fila.TipoUsuario,
                        FechaUltimoAcceso = fila.UltimoIngreso,
                        CreatedBy         = _user.Email
                    };
                    await _usuarioRepo.AddAsync(usr, ct); // estado: Added → no llamar Update
                    usuarios[fila.UsuarioSAP.ToUpper()] = usr;
                }
                else
                {
                    // Modificar propiedades directamente: EF detecta "Modified" al SaveChanges
                    if (!string.IsNullOrWhiteSpace(fila.Cedula))         usr.Cedula         = fila.Cedula;
                    if (!string.IsNullOrWhiteSpace(fila.NombreCompleto)) usr.NombreCompleto = fila.NombreCompleto;
                    if (!string.IsNullOrWhiteSpace(fila.Sociedad))       usr.Sociedad       = fila.Sociedad;
                    if (!string.IsNullOrWhiteSpace(fila.Departamento))   usr.Departamento   = fila.Departamento;
                    if (!string.IsNullOrWhiteSpace(fila.Puesto))         usr.Puesto         = fila.Puesto;
                    if (!string.IsNullOrWhiteSpace(fila.Email))          usr.Email          = fila.Email;
                    usr.Estado     = fila.Estado;
                    usr.TipoUsuario = fila.TipoUsuario;
                    if (fila.UltimoIngreso.HasValue) usr.FechaUltimoAcceso = fila.UltimoIngreso;
                    usr.UpdatedAt  = DateTime.UtcNow;
                }

                if (string.IsNullOrWhiteSpace(fila.Rol)) continue;

                // 2. Upsert RolSistema — igual, solo modificar propiedades
                if (!roles.TryGetValue(fila.Rol.ToUpper(), out var rol))
                {
                    rol = new RolSistema
                    {
                        Sistema                  = request.Sistema,
                        NombreRol                = fila.Rol.ToUpper(),
                        Descripcion              = fila.DescripcionRol,
                        NivelRiesgo              = fila.NivelRiesgo,
                        EsCritico                = fila.EsCritico,
                        TransaccionesAutorizadas = fila.Transacciones,
                    };
                    await _rolRepo.AddAsync(rol, ct);
                    roles[fila.Rol.ToUpper()] = rol;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(fila.Transacciones))  rol.TransaccionesAutorizadas = fila.Transacciones;
                    if (!string.IsNullOrWhiteSpace(fila.DescripcionRol)) rol.Descripcion = fila.DescripcionRol;
                    if (fila.EsCritico) rol.EsCritico = true;
                }

                pendientesAsign.Add((usr, rol, fila));
                pendientesBatch++;

                // Guardar en lotes de BATCH filas para manejar 15.000+ registros
                if (pendientesBatch >= BATCH)
                {
                    try { await _usuarioRepo.SaveChangesAsync(ct); pendientesBatch = 0; }
                    catch (Exception bex)
                    {
                        resultado.DetalleErrores.Add($"Error en lote (fila ~{idx}): {bex.InnerException?.Message ?? bex.Message}");
                        pendientesBatch = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                resultado.Errores++;
                resultado.DetalleErrores.Add($"Fila {idx}: {ex.Message}");
            }
        }

        // Guardar último lote de usuarios/roles
        try { await _usuarioRepo.SaveChangesAsync(ct); }
        catch (Exception ex)
        {
            resultado.DetalleErrores.Insert(0, $"Error al guardar usuarios/roles: {ex.InnerException?.Message ?? ex.Message}");
            resultado.Errores += pendientesAsign.Count;
            return resultado;
        }

        // ── PASADA 2: AsignacionRolUsuario ────────────────────────────────────
        // Ahora todos los usuarios y roles existen en BD → no habrá FK violation
        int asignBatch = 0;
        foreach (var (usr, rol, fila) in pendientesAsign)
        {
            try
            {
                var rolesUsuario = asignaciones.GetValueOrDefault(usr.Id) ?? [];
                if (!rolesUsuario.Contains(rol.Id))
                {
                    await _asignRepo.AddAsync(new AsignacionRolUsuario
                    {
                        UsuarioId        = usr.Id,
                        RolId            = rol.Id,
                        FechaAsignacion  = fila.FechaDesde,
                        FechaVencimiento = fila.FechaHasta,
                        AsignadoPor      = "CARGA_SAP",
                        Activa           = true
                    }, ct);
                    rolesUsuario.Add(rol.Id);
                    asignaciones[usr.Id] = rolesUsuario;
                    asignBatch++;
                }
                resultado.Insertados++;

                if (asignBatch >= BATCH)
                {
                    try { await _asignRepo.SaveChangesAsync(ct); asignBatch = 0; }
                    catch (Exception bex)
                    {
                        resultado.DetalleErrores.Add($"Error lote asignaciones: {bex.InnerException?.Message ?? bex.Message}");
                        asignBatch = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                resultado.Errores++;
                resultado.DetalleErrores.Add($"Asignación {usr.NombreUsuario}/{rol.NombreRol}: {ex.Message}");
            }
        }

        // Guardar último lote de asignaciones
        try { await _asignRepo.SaveChangesAsync(ct); }
        catch (Exception ex)
        {
            resultado.DetalleErrores.Insert(0, $"Error al guardar asignaciones: {ex.InnerException?.Message ?? ex.Message}");
        }

        await _audit.LogAsync(_user.UserId, _user.Email, "CARGA_ROLES_SAP", "AsignacionRolUsuario",
            null, datosDespues: new { Sistema = request.Sistema, resultado.Insertados, resultado.Errores }, ct: ct);

        return resultado;
    }

    // Columnas V_SAP_USR_RECERTIFICAION (con ID como primera columna):
    // 1=ID(Cedula), 2=USUARIO, 3=NOMBRE_COMPLETO, 4=SOCIEDAD, 5=DEPARTAMENTO,
    // 6=PUESTO, 7=EMAIL, 8=ROL, 9=INICIO_VALIDEZ, 10=FIN_VALIDEZ,
    // 11=TRANSACCION, 12=ULTIMO_INGRESO
    // Una fila por ID+USUARIO+ROL+TRANSACCION — agrupamos por usuario+rol
    internal static List<FilaRolSAP> ParseExcel(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        // Detectar si la primera columna es ID/Cedula o directamente USUARIO
        // Si el encabezado de col 1 contiene "ID" asumimos que hay cédula
        var header1 = ws.Cell(1, 1).GetString().Trim().ToUpperInvariant();
        bool tieneCedula = header1 == "ID" || header1.Contains("CEDULA");
        int offset = tieneCedula ? 1 : 0;  // si hay cédula, las demás columnas se desplazan +1

        var dict = new Dictionary<(string, string), FilaRolSAP>();
        var tcodes = new Dictionary<(string, string), List<string>>();

        for (int r = 2; r <= lastRow; r++)
        {
            string cedula = tieneCedula ? ws.Cell(r, 1).GetString().Trim() : string.Empty;
            var usuario = ws.Cell(r, 1 + offset).GetString().Trim();
            if (string.IsNullOrWhiteSpace(usuario) || usuario.StartsWith('*')) continue;

            var rol        = ws.Cell(r, 7 + offset).GetString().Trim();
            var transaccion = ws.Cell(r, 10 + offset).GetString().Trim();
            var key        = (usuario.ToUpper(), rol.ToUpper());

            if (!dict.ContainsKey(key))
            {
                dict[key] = new FilaRolSAP
                {
                    Cedula         = string.IsNullOrWhiteSpace(cedula) ? null : cedula,
                    UsuarioSAP     = usuario.ToUpper(),
                    NombreCompleto = ws.Cell(r, 2 + offset).GetString().Trim(),
                    Sociedad       = ws.Cell(r, 3 + offset).GetString().Trim(),
                    Departamento   = ws.Cell(r, 4 + offset).GetString().Trim(),
                    Puesto         = ws.Cell(r, 5 + offset).GetString().Trim(),
                    Email          = ws.Cell(r, 6 + offset).GetString().Trim(),
                    Rol            = rol,
                    FechaDesde     = ws.Cell(r, 8 + offset).TryGetValue<DateTime>(out var fd) ? DateOnly.FromDateTime(fd) : null,
                    FechaHasta     = ws.Cell(r, 9 + offset).TryGetValue<DateTime>(out var fh) ? DateOnly.FromDateTime(fh) : null,
                    UltimoIngreso  = ws.Cell(r, 11 + offset).TryGetValue<DateTime>(out var ui) ? ui : null,
                };
                tcodes[key] = [];
            }
            else if (string.IsNullOrWhiteSpace(dict[key].Cedula) && !string.IsNullOrWhiteSpace(cedula))
                dict[key].Cedula = cedula;

            if (!string.IsNullOrWhiteSpace(transaccion))
                tcodes[key].Add(transaccion);
        }

        foreach (var (k, fila) in dict)
            fila.Transacciones = tcodes.TryGetValue(k, out var list) && list.Count > 0
                ? string.Join(",", list.Distinct(StringComparer.OrdinalIgnoreCase))
                : null;

        return [.. dict.Values];
    }

    internal class FilaRolSAP
    {
        public string? Cedula { get; set; }
        public string UsuarioSAP { get; set; } = string.Empty;
        public string? NombreCompleto { get; set; }
        public string? Sociedad { get; set; }
        public string? Departamento { get; set; }
        public string? Puesto { get; set; }
        public string? Email { get; set; }
        public EstadoUsuario Estado { get; set; } = EstadoUsuario.ACTIVO;
        public string? TipoUsuario { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string? DescripcionRol { get; set; }
        public string? NivelRiesgo { get; set; }
        public bool EsCritico { get; set; }
        public DateOnly? FechaDesde { get; set; }
        public DateOnly? FechaHasta { get; set; }
        public DateTime? UltimoIngreso { get; set; }
        public string? Transacciones { get; set; }
    }
}

// ─── Carga de Matriz de Puestos SAP (aprobada por Contraloría) ──────────────

public record CargarMatrizPuestosCommand(
    Stream Contenido,
    string NombreArchivo,
    string ContentType
) : IRequest<CargaResultado>;

public class CargarMatrizPuestosHandler : IRequestHandler<CargarMatrizPuestosCommand, CargaResultado>
{
    private readonly IRepository<MatrizPuestoSAP> _repo;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;

    public CargarMatrizPuestosHandler(IRepository<MatrizPuestoSAP> repo, ICurrentUserService user, IAuditLoggerService audit)
    { _repo = repo; _user = user; _audit = audit; }

    public async Task<CargaResultado> Handle(CargarMatrizPuestosCommand request, CancellationToken ct)
    {
        const int BATCH = 500;
        var filas = ParseExcelMatriz(request.Contenido);
        var resultado = new CargaResultado { TotalRegistros = filas.Count };

        // Upsert por (UsuarioSAP, Rol, Transaccion) — una fila por permiso
        var existentes = (await _repo.GetAllAsync(ct))
            .GroupBy(m => $"{m.UsuarioSAP.ToUpper()}|{m.Rol.ToUpper()}|{m.Transaccion?.ToUpper()}")
            .ToDictionary(g => g.Key, g => g.First());

        int pendientes = 0;
        foreach (var (fila, idx) in filas.Select((f, i) => (f, i + 2)))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fila.UsuarioSAP) || string.IsNullOrWhiteSpace(fila.Rol))
                {
                    resultado.Errores++;
                    resultado.DetalleErrores.Add($"Fila {idx}: USUARIO o ROL vacío.");
                    continue;
                }

                var clave = $"{fila.UsuarioSAP.ToUpper()}|{fila.Rol.ToUpper()}|{fila.Transaccion?.ToUpper()}";

                if (!existentes.TryGetValue(clave, out var reg))
                {
                    reg = new MatrizPuestoSAP
                    {
                        Cedula                   = fila.Cedula,
                        UsuarioSAP               = fila.UsuarioSAP,
                        NombreCompleto           = fila.NombreCompleto,
                        Sociedad                 = fila.Sociedad,
                        Departamento             = fila.Departamento,
                        Puesto                   = fila.Puesto ?? string.Empty,
                        Email                    = fila.Email,
                        Rol                      = fila.Rol,
                        InicioValidez            = fila.InicioValidez,
                        FinValidez               = fila.FinValidez,
                        Transaccion              = fila.Transaccion,
                        UltimoIngreso            = fila.UltimoIngreso,
                        FechaRevisionContraloria = fila.FechaRevisionContraloria,
                        CreatedBy                = _user.Email
                    };
                    await _repo.AddAsync(reg, ct);
                    resultado.Insertados++;
                }
                else
                {
                    reg.Puesto                   = fila.Puesto ?? reg.Puesto;
                    reg.NombreCompleto           = fila.NombreCompleto ?? reg.NombreCompleto;
                    reg.Departamento             = fila.Departamento ?? reg.Departamento;
                    reg.InicioValidez            = fila.InicioValidez ?? reg.InicioValidez;
                    reg.FinValidez               = fila.FinValidez ?? reg.FinValidez;
                    reg.UltimoIngreso            = fila.UltimoIngreso ?? reg.UltimoIngreso;
                    reg.FechaRevisionContraloria = fila.FechaRevisionContraloria ?? reg.FechaRevisionContraloria;
                    reg.UpdatedAt                = DateTime.UtcNow;
                    resultado.Actualizados++;
                }

                pendientes++;
                if (pendientes >= BATCH)
                {
                    try { await _repo.SaveChangesAsync(ct); pendientes = 0; }
                    catch (Exception ex)
                    {
                        resultado.DetalleErrores.Add($"Error batch fila ~{idx}: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                resultado.Errores++;
                resultado.DetalleErrores.Add($"Fila {idx}: {ex.Message}");
            }
        }

        if (pendientes > 0)
        {
            try { await _repo.SaveChangesAsync(ct); }
            catch (Exception ex)
            {
                resultado.DetalleErrores.Insert(0, $"Error al guardar: {ex.InnerException?.Message ?? ex.Message}");
                resultado.Errores += resultado.Insertados + resultado.Actualizados;
                resultado.Insertados = resultado.Actualizados = 0;
                return resultado;
            }
        }

        await _audit.LogAsync(_user.UserId, _user.Email, "CARGA_MATRIZ_PUESTOS", "MatrizPuestoSAP",
            null, datosDespues: new { resultado.Insertados, resultado.Actualizados }, ct: ct);

        return resultado;
    }

    // Parsea el Excel de SAP: una fila por USUARIO+ROL+TRANSACCION
    // Columnas: ID | USUARIO | NOMBRE_COMPLETO | SOCIEDAD | DEPARTAMENTO | PUESTO |
    //           EMAIL | ROL | INICIO_VALID | FIN_VALID | TRANSACCION | ULTIMO_INGRESO | FECHA_REVISION_CONTRALORIA
    // Las fechas pueden venir como número YYYYMMDD (ej: 20250619) o como fecha Excel
    private static List<FilaMatriz> ParseExcelMatriz(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        // Detectar si la col 1 es ID/Cédula o directamente USUARIO
        var header1 = ws.Cell(1, 1).GetString().Trim().ToUpperInvariant();
        bool tieneCedula = header1 == "ID" || header1.Contains("CEDULA") || header1.Contains("CÉDULA");
        int off = tieneCedula ? 1 : 0;

        var resultado = new List<FilaMatriz>(lastRow);
        for (int r = 2; r <= lastRow; r++)
        {
            var usuario = ws.Cell(r, 1 + off).GetString().Trim();
            if (string.IsNullOrWhiteSpace(usuario)) continue;

            resultado.Add(new FilaMatriz
            {
                Cedula                   = tieneCedula ? Ni(ws.Cell(r, 1).GetString()) : null,
                UsuarioSAP               = usuario.ToUpper(),
                NombreCompleto           = Ni(ws.Cell(r, 2 + off).GetString()),
                Sociedad                 = Ni(ws.Cell(r, 3 + off).GetString()),
                Departamento             = Ni(ws.Cell(r, 4 + off).GetString()),
                Puesto                   = Ni(ws.Cell(r, 5 + off).GetString()),
                Email                    = Ni(ws.Cell(r, 6 + off).GetString()),
                Rol                      = ws.Cell(r, 7 + off).GetString().Trim().ToUpper(),
                InicioValidez            = ParseFechaSAP(ws.Cell(r, 8 + off)),
                FinValidez               = ParseFechaSAP(ws.Cell(r, 9 + off)),
                Transaccion              = Ni(ws.Cell(r, 10 + off).GetString()),
                UltimoIngreso            = ParseFechaSAP(ws.Cell(r, 11 + off)) is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : null,
                FechaRevisionContraloria = ParseFechaSAP(ws.Cell(r, 12 + off)),
            });
        }
        return resultado;
    }

    private static string? Ni(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // Maneja fechas Excel, YYYYMMDD como número entero, y texto YYYYMMDD
    private static DateOnly? ParseFechaSAP(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        // Fecha Excel real
        if (cell.TryGetValue<DateTime>(out var dt)) return DateOnly.FromDateTime(dt);

        // Número entero YYYYMMDD (ej: 20250619 o 99991231)
        if (cell.TryGetValue<long>(out var num) && num >= 10000101 && num <= 99991231)
        {
            int y = (int)(num / 10000), m = (int)(num % 10000 / 100), d = (int)(num % 100);
            if (m is >= 1 and <= 12 && d is >= 1 and <= 31)
            {
                // FIN_VALID=99991231 → sin vencimiento → null
                if (y == 9999) return null;
                try { return new DateOnly(y, m, d); } catch { }
            }
        }

        // Texto YYYYMMDD
        var txt = cell.GetString().Trim();
        if (txt.Length == 8 && int.TryParse(txt, out var n))
        {
            int y = n / 10000, m = n % 10000 / 100, d2 = n % 100;
            if (y == 9999) return null;
            try { return new DateOnly(y, m, d2); } catch { }
        }

        return null;
    }

    private class FilaMatriz
    {
        public string? Cedula { get; set; }
        public string UsuarioSAP { get; set; } = string.Empty;
        public string? NombreCompleto { get; set; }
        public string? Sociedad { get; set; }
        public string? Departamento { get; set; }
        public string? Puesto { get; set; }
        public string? Email { get; set; }
        public string Rol { get; set; } = string.Empty;
        public DateOnly? InicioValidez { get; set; }
        public DateOnly? FinValidez { get; set; }
        public string? Transaccion { get; set; }
        public DateTime? UltimoIngreso { get; set; }
        public DateOnly? FechaRevisionContraloria { get; set; }
    }
}

// ─── Carga de Casos SE Suite ────────────────────────────────────────────────

public record CargarCasosSESuiteCommand(
    Stream Contenido,
    string NombreArchivo,
    string ContentType
) : IRequest<CargaResultado>;

public class CargarCasosSESuiteHandler : IRequestHandler<CargarCasosSESuiteCommand, CargaResultado>
{
    private readonly IRepository<CasoSESuite> _repo;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;

    public CargarCasosSESuiteHandler(IRepository<CasoSESuite> repo, ICurrentUserService user, IAuditLoggerService audit)
    { _repo = repo; _user = user; _audit = audit; }

    public async Task<CargaResultado> Handle(CargarCasosSESuiteCommand request, CancellationToken ct)
    {
        // Columnas: 1=NUMERO_CASO, 2=TITULO, 3=USUARIO_SAP, 4=CEDULA,
        //           5=ROL_JUSTIFICADO, 6=TRANSACCIONES, 7=FECHA_APROBACION,
        //           8=FECHA_VENCIMIENTO, 9=ESTADO, 10=APROBADOR
        var filas = new List<FilaCaso>();
        using var wb = new XLWorkbook(request.Contenido);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 2; r <= lastRow; r++)
        {
            var num = ws.Cell(r, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(num) || num.StartsWith('*')) continue;
            filas.Add(new FilaCaso
            {
                NumeroCaso = num,
                Titulo     = ws.Cell(r, 2).GetString().Trim(),
                UsuarioSAP = ws.Cell(r, 3).GetString().Trim(),
                Cedula     = ws.Cell(r, 4).GetString().Trim(),
                RolJustificado = ws.Cell(r, 5).GetString().Trim(),
                Transacciones  = ws.Cell(r, 6).GetString().Trim(),
                FechaAprobacion   = ws.Cell(r, 7).TryGetValue<DateTime>(out var fa) ? DateOnly.FromDateTime(fa) : null,
                FechaVencimiento  = ws.Cell(r, 8).TryGetValue<DateTime>(out var fv) ? DateOnly.FromDateTime(fv) : null,
                Estado     = ws.Cell(r, 9).GetString().Trim() is { Length: > 0 } e ? e : "APROBADO",
                Aprobador  = ws.Cell(r, 10).GetString().Trim(),
            });
        }

        const int BATCH = 500;
        var resultado = new CargaResultado { TotalRegistros = filas.Count };
        var existentes = (await _repo.GetAllAsync(ct)).ToDictionary(c => c.NumeroCaso.ToUpper());

        int pendientes = 0;
        foreach (var (fila, idx) in filas.Select((f, i) => (f, i + 2)))
        {
            try
            {
                if (!existentes.TryGetValue(fila.NumeroCaso.ToUpper(), out var caso))
                {
                    caso = new CasoSESuite
                    {
                        NumeroCaso = fila.NumeroCaso, Titulo = fila.Titulo,
                        UsuarioSAP = fila.UsuarioSAP.ToUpperInvariant(),
                        Cedula = string.IsNullOrWhiteSpace(fila.Cedula) ? null : fila.Cedula,
                        RolJustificado = fila.RolJustificado, TransaccionesJustificadas = fila.Transacciones,
                        FechaAprobacion = fila.FechaAprobacion, FechaVencimiento = fila.FechaVencimiento,
                        EstadoCaso = fila.Estado, Aprobador = fila.Aprobador, CreatedBy = _user.Email
                    };
                    await _repo.AddAsync(caso, ct);
                    resultado.Insertados++;
                }
                else
                {
                    // IMPORTANTE: no llamar UpdateAsync — solo modificar propiedades.
                    caso.EstadoCaso      = fila.Estado;
                    caso.FechaVencimiento = fila.FechaVencimiento;
                    caso.Aprobador       = fila.Aprobador;
                    caso.UpdatedAt       = DateTime.UtcNow;
                    resultado.Actualizados++;
                }

                pendientes++;
                if (pendientes >= BATCH)
                {
                    try { await _repo.SaveChangesAsync(ct); pendientes = 0; }
                    catch (Exception ex)
                    {
                        resultado.DetalleErrores.Add($"Error batch fila ~{idx}: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                resultado.Errores++;
                resultado.DetalleErrores.Add($"Fila {idx} ({fila.NumeroCaso}): {ex.Message}");
            }
        }

        if (pendientes > 0)
        {
            try { await _repo.SaveChangesAsync(ct); }
            catch (Exception ex)
            {
                resultado.DetalleErrores.Insert(0, $"Error al guardar: {ex.InnerException?.Message ?? ex.Message}");
                resultado.Errores += resultado.Insertados + resultado.Actualizados;
                resultado.Insertados = resultado.Actualizados = 0;
                return resultado;
            }
        }

        await _audit.LogAsync(_user.UserId, _user.Email, "CARGA_CASOS_SESUITE", "CasoSESuite",
            null, datosDespues: new { resultado.Insertados, resultado.Actualizados }, ct: ct);
        return resultado;
    }

    private class FilaCaso
    {
        public string NumeroCaso { get; set; } = string.Empty;
        public string? Titulo { get; set; }
        public string UsuarioSAP { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public string? RolJustificado { get; set; }
        public string? Transacciones { get; set; }
        public DateOnly? FechaAprobacion { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public string Estado { get; set; } = "APROBADO";
        public string? Aprobador { get; set; }
    }
}
