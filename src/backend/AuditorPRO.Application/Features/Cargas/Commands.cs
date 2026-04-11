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

        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(_user.UserId, _user.Email, "CARGA_EMPLEADOS", "EmpleadoMaestro",
            null, datosDespues: new { resultado.Insertados, resultado.Actualizados, resultado.Errores }, ct: ct);

        return resultado;
    }

    private static List<FilaEmpleado> ParseExcel(Stream stream)
    {
        var filas = new List<FilaEmpleado>();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            filas.Add(new FilaEmpleado
            {
                NumeroEmpleado = ws.Cell(r, 1).GetString(),
                NombreCompleto = ws.Cell(r, 2).GetString(),
                Email = ws.Cell(r, 3).GetString(),
                Puesto = ws.Cell(r, 4).GetString(),
                Activo = ws.Cell(r, 5).GetString().Equals("ACTIVO", StringComparison.OrdinalIgnoreCase),
                FechaIngreso = ws.Cell(r, 6).TryGetValue<DateTime>(out var d) ? d : null
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
