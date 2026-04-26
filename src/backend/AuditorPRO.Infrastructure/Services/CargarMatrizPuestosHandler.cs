using AuditorPRO.Application.Features.Cargas;
using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Interfaces;
using AuditorPRO.Infrastructure.Persistence;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuditorPRO.Infrastructure.Services;

// Estrategia: DELETE todos los registros existentes + INSERT en lotes.
// Evita GetAllAsync (16k+ registros en memoria) y la lógica de upsert por clave compuesta.
// La Matriz de Puestos es un snapshot completo aprobado por Contraloría — siempre se reemplaza.
public class CargarMatrizPuestosHandler : IRequestHandler<CargarMatrizPuestosCommand, CargaResultado>
{
    private readonly AppDbContext _db;
    private readonly IRepository<LoteCarga> _lotes;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;
    private readonly ILogger<CargarMatrizPuestosHandler> _logger;

    public CargarMatrizPuestosHandler(
        AppDbContext db,
        IRepository<LoteCarga> lotes,
        ICurrentUserService user,
        IAuditLoggerService audit,
        ILogger<CargarMatrizPuestosHandler> logger)
    {
        _db = db; _lotes = lotes; _user = user; _audit = audit; _logger = logger;
    }

    public async Task<CargaResultado> Handle(CargarMatrizPuestosCommand request, CancellationToken ct)
    {
        const int BATCH = 1000;

        var filas = ParseExcelMatriz(request.Contenido);
        var resultado = new CargaResultado { TotalRegistros = filas.Count };

        _logger.LogInformation("CargarMatriz: {Total} filas parseadas. Eliminando registros anteriores...", filas.Count);

        // Reemplazar completamente la matriz — DELETE directo evita cargar 16k entidades
        var eliminados = await _db.Database.ExecuteSqlRawAsync("DELETE FROM [MatrizPuestosSAP]", ct);
        _logger.LogInformation("CargarMatriz: {N} registros eliminados. Insertando nueva matriz...", eliminados);

        var batch = new List<MatrizPuestoSAP>(BATCH);

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

                batch.Add(new MatrizPuestoSAP
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
                });
                resultado.Insertados++;

                if (batch.Count >= BATCH)
                {
                    await _db.MatrizPuestosSAP.AddRangeAsync(batch, ct);
                    await _db.SaveChangesAsync(ct);
                    _logger.LogInformation("CargarMatriz: lote guardado, fila ~{Idx}", idx);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                resultado.Errores++;
                resultado.DetalleErrores.Add($"Fila {idx}: {ex.Message}");
            }
        }

        if (batch.Count > 0)
        {
            try
            {
                await _db.MatrizPuestosSAP.AddRangeAsync(batch, ct);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                resultado.DetalleErrores.Insert(0, $"Error al guardar último lote: {ex.InnerException?.Message ?? ex.Message}");
                resultado.Errores += batch.Count;
                resultado.Insertados -= batch.Count;
                return resultado;
            }
        }

        var lote = await LoteHelper.CrearLoteAsync(
            _lotes, "MATRIZ_PUESTOS",
            request.SociedadCodigo, request.SociedadNombre,
            request.NombreArchivo, resultado, _user.Email, ct);

        resultado.LoteId         = lote.Id;
        resultado.FechaCarga     = lote.FechaCarga;
        resultado.SociedadCodigo = lote.SociedadCodigo;
        resultado.SociedadNombre = lote.SociedadNombre;

        await _audit.LogAsync(_user.UserId, _user.Email, "CARGA_MATRIZ_PUESTOS", "MatrizPuestoSAP",
            null, datosDespues: new { resultado.Insertados, resultado.Errores }, ct: ct);

        _logger.LogInformation("CargarMatriz: completado — {I} insertados, {E} errores", resultado.Insertados, resultado.Errores);
        return resultado;
    }

    // Columnas: ID | USUARIO | NOMBRE_COMPLETO | SOCIEDAD | DEPARTAMENTO | PUESTO |
    //           EMAIL | ROL | INICIO_VALID | FIN_VALID | TRANSACCION | ULTIMO_INGRESO | FECHA_REVISION_CONTRALORIA
    private static List<FilaMatriz> ParseExcelMatriz(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

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

    private static DateOnly? ParseFechaSAP(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        if (cell.TryGetValue<DateTime>(out var dt)) return DateOnly.FromDateTime(dt);

        if (cell.TryGetValue<long>(out var num) && num >= 10000101 && num <= 99991231)
        {
            int y = (int)(num / 10000), m = (int)(num % 10000 / 100), d = (int)(num % 100);
            if (m is >= 1 and <= 12 && d is >= 1 and <= 31)
            {
                if (y == 9999) return null;
                try { return new DateOnly(y, m, d); } catch { }
            }
        }

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
