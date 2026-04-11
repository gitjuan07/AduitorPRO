using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Interfaces;
using AuditorPRO.Infrastructure.Persistence;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AuditorPRO.Infrastructure.Services;

public class IngestorDocumentosService : IIngestorDocumentosService
{
    private static readonly string[] ExtensionesPermitidas =
        [".pdf", ".docx", ".doc", ".xlsx", ".xls", ".csv", ".txt", ".md"];

    private readonly AppDbContext _db;
    private readonly ILogger<IngestorDocumentosService> _logger;

    public IngestorDocumentosService(AppDbContext db, ILogger<IngestorDocumentosService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── Ingestión por directorio ──────────────────────────────────────────────
    public async Task<IngestResultado> IngestirDirectorioAsync(
        string rutaDirectorio, string? usuario, CancellationToken ct = default)
    {
        if (!Directory.Exists(rutaDirectorio))
            return new IngestResultado(0, 0, 0, [$"El directorio no existe: {rutaDirectorio}"]);

        var archivos = Directory.EnumerateFiles(rutaDirectorio, "*.*", SearchOption.AllDirectories)
            .Where(f => ExtensionesPermitidas.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        int procesados = 0, errores = 0, omitidos = 0;
        var detalles = new List<string>();

        foreach (var archivo in archivos)
        {
            if (ct.IsCancellationRequested) break;

            // Omitir si ya existe
            var yaExiste = await _db.BaseConocimiento
                .AnyAsync(b => b.RutaOriginal == archivo && !b.IsDeleted, ct);
            if (yaExiste) { omitidos++; continue; }

            var resultado = await IngestirArchivoAsync(archivo, usuario, ct);
            procesados += resultado.Procesados;
            errores    += resultado.Errores;
            detalles.AddRange(resultado.Detalles);
        }

        detalles.Insert(0, $"Directorio: {rutaDirectorio} | Archivos encontrados: {archivos.Count}");
        return new IngestResultado(procesados, errores, omitidos, detalles);
    }

    // ─── Ingestión de un archivo por ruta ─────────────────────────────────────
    public async Task<IngestResultado> IngestirArchivoAsync(
        string rutaArchivo, string? usuario, CancellationToken ct = default)
    {
        try
        {
            await using var stream = File.OpenRead(rutaArchivo);
            var nombre = Path.GetFileName(rutaArchivo);
            var resultado = await ProcesarStreamAsync(stream, nombre, rutaArchivo, usuario, ct);
            return new IngestResultado(1, 0, 0, [$"OK: {nombre}"]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error ingiriendo {Archivo}", rutaArchivo);
            return new IngestResultado(0, 1, 0, [$"ERROR: {Path.GetFileName(rutaArchivo)} — {ex.Message}"]);
        }
    }

    // ─── Ingestión desde stream (upload) ──────────────────────────────────────
    public async Task<IngestResultado> IngestirStreamAsync(
        Stream stream, string nombreArchivo, string? usuario, CancellationToken ct = default)
    {
        try
        {
            await ProcesarStreamAsync(stream, nombreArchivo, "UPLOAD", usuario, ct);
            return new IngestResultado(1, 0, 0, [$"OK: {nombreArchivo}"]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error ingiriendo stream {Nombre}", nombreArchivo);
            return new IngestResultado(0, 1, 0, [$"ERROR: {nombreArchivo} — {ex.Message}"]);
        }
    }

    // ─── Núcleo de procesamiento ───────────────────────────────────────────────
    private async Task<BaseConocimiento> ProcesarStreamAsync(
        Stream stream, string nombre, string ruta, string? usuario, CancellationToken ct)
    {
        var ext = Path.GetExtension(nombre).ToLowerInvariant();
        var texto = ext switch
        {
            ".pdf"  => ExtraerTextoPdf(stream),
            ".docx" => ExtraerTextoDocx(stream),
            ".xlsx" => ExtraerTextoXlsx(stream),
            ".csv"  => ExtraerTextoCsv(stream),
            ".txt" or ".md" => ExtraerTextoPlano(stream),
            _ => string.Empty
        };

        texto = LimpiarTexto(texto);
        var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var (dominio, controles, tags) = ClasificarContenido(texto, nombre);

        var doc = new BaseConocimiento
        {
            NombreArchivo      = nombre,
            RutaOriginal       = ruta,
            TipoArchivo        = ext.TrimStart('.').ToUpperInvariant(),
            TamanoBytes        = stream.CanSeek ? stream.Length : 0,
            TextoCompleto      = texto.Length > 500_000 ? texto[..500_000] : texto, // max 500KB de texto
            TotalPalabras      = palabras,
            DominioDetectado   = dominio,
            ControlesDetectados= controles,
            Tags               = tags,
            Estado             = "PROCESADO",
            FuenteIngesta      = ruta == "UPLOAD" ? "UPLOAD" : "DIRECTORIO",
            IngresadoPor       = usuario,
            CreadoAt           = DateTime.UtcNow
        };

        _db.BaseConocimiento.Add(doc);
        await _db.SaveChangesAsync(ct);
        return doc;
    }

    // ─── Extracción de texto por tipo ─────────────────────────────────────────
    private static string ExtraerTextoPdf(Stream stream)
    {
        var sb = new StringBuilder();
        // PdfPig requiere un stream buscable
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        using var pdf = PdfDocument.Open(ms);
        foreach (var page in pdf.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtraerTextoDocx(Stream stream)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return string.Empty;
        foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            sb.AppendLine(para.InnerText);
        return sb.ToString();
    }

    private static string ExtraerTextoXlsx(Stream stream)
    {
        var sb = new StringBuilder();
        using var doc = SpreadsheetDocument.Open(stream, false);
        var sheets = doc.WorkbookPart?.Workbook.Sheets?.Elements<Sheet>() ?? [];
        foreach (var sheet in sheets)
        {
            var part = doc.WorkbookPart?.GetPartById(sheet.Id!) as WorksheetPart;
            if (part == null) continue;
            sb.AppendLine($"--- Hoja: {sheet.Name} ---");
            var rows = part.Worksheet.Descendants<Row>();
            foreach (var row in rows)
            {
                var valores = row.Elements<Cell>()
                    .Select(c => ObtenerValorCelda(doc, c))
                    .Where(v => !string.IsNullOrWhiteSpace(v));
                sb.AppendLine(string.Join("\t", valores));
            }
        }
        return sb.ToString();
    }

    private static string ObtenerValorCelda(SpreadsheetDocument doc, Cell cell)
    {
        var valor = cell.InnerText;
        if (cell.DataType?.Value == CellValues.SharedString)
        {
            var sst = doc.WorkbookPart?.SharedStringTablePart?.SharedStringTable;
            if (sst != null && int.TryParse(valor, out var idx))
                valor = sst.ElementAt(idx).InnerText;
        }
        return valor;
    }

    private static string ExtraerTextoCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string ExtraerTextoPlano(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    // ─── Limpieza de texto ─────────────────────────────────────────────────────
    private static string LimpiarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        texto = Regex.Replace(texto, @"\s{3,}", "  ");
        texto = Regex.Replace(texto, @"[^\S\r\n]{2,}", " ");
        return texto.Trim();
    }

    // ─── Clasificación automática por contenido ────────────────────────────────
    private static readonly Dictionary<string, string[]> DominioKeywords = new()
    {
        ["ID"]       = ["usuario", "acceso", "identidad", "alta", "baja", "empleado", "inactivo", "bloqueado", "desvinculación"],
        ["CHG"]      = ["cambio", "expediente", "transporte", "ticket", "aprobación", "cambios", "change"],
        ["SAP-SEC"]  = ["sap", "rol", "perfil", "se38", "su01", "su10", "bapi", "rfc", "transport"],
        ["RECERT"]   = ["recertificación", "campaña", "validación", "jefatura", "certificación", "revisión acceso"],
        ["SoD"]      = ["segregación", "conflicto", "sod", "segregation", "incompatible"],
        ["EVID"]     = ["evidencia", "respaldo", "documento", "adjunto", "comprobante", "soporte"],
        ["DOC"]      = ["política", "procedimiento", "norma", "lineamiento", "manual", "guía"],
        ["BCK"]      = ["respaldo", "backup", "restauración", "recovery", "recuperación"],
        ["NET"]      = ["red", "firewall", "vpn", "seguridad perimetral", "network"],
        ["CMP"]      = ["cumplimiento", "iso", "cobit", "sox", "itil", "auditoría"],
    };

    private static readonly string[] ControlPattern =
        ["ID-", "ABC-", "RECERT-", "SAP-", "CAMBIOS-", "EVID-", "DOC-", "BCK-", "NET-", "SOD-", "CFG-", "CHG-"];

    private static (string? dominio, string? controles, string? tags) ClasificarContenido(string texto, string nombre)
    {
        var textoLower = texto.ToLowerInvariant();
        var nombreLower = nombre.ToLowerInvariant();

        // Detectar dominio
        string? dominio = null;
        int maxScore = 0;
        foreach (var (dom, keywords) in DominioKeywords)
        {
            var score = keywords.Count(kw => textoLower.Contains(kw) || nombreLower.Contains(kw));
            if (score > maxScore) { maxScore = score; dominio = dom; }
        }

        // Detectar códigos de control mencionados
        var controlesMencionados = new HashSet<string>();
        foreach (var prefix in ControlPattern)
        {
            var matches = Regex.Matches(texto, $@"\b{Regex.Escape(prefix)}\d{{3}}\b");
            foreach (Match m in matches) controlesMencionados.Add(m.Value);
        }
        var controlesJson = controlesMencionados.Count > 0
            ? JsonSerializer.Serialize(controlesMencionados.ToList())
            : null;

        // Tags de palabras frecuentes relevantes
        var todasKeywords = DominioKeywords.Values.SelectMany(v => v).Distinct();
        var tagsEncontrados = todasKeywords.Where(kw => textoLower.Contains(kw)).Take(10).ToList();
        var tagsJson = tagsEncontrados.Count > 0
            ? JsonSerializer.Serialize(tagsEncontrados)
            : null;

        return (dominio, controlesJson, tagsJson);
    }

    // ─── Búsqueda semántica simple (keyword + scoring) ─────────────────────────
    public async Task<List<BaseConocimiento>> BuscarAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        var terminos = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 3)
            .Distinct()
            .ToList();

        if (!terminos.Any())
            return await _db.BaseConocimiento
                .Where(b => !b.IsDeleted && b.Estado == "PROCESADO")
                .OrderByDescending(b => b.CreadoAt)
                .Take(topK)
                .ToListAsync(ct);

        // SQLite: buscar documentos que contengan los términos
        var docs = await _db.BaseConocimiento
            .Where(b => !b.IsDeleted && b.Estado == "PROCESADO")
            .ToListAsync(ct);

        // Scoring en memoria: número de términos encontrados + frecuencia
        var scored = docs
            .Select(d =>
            {
                var texto = (d.TextoCompleto + " " + d.NombreArchivo + " " + d.Tags).ToLowerInvariant();
                var score = terminos.Sum(t => CountOccurrences(texto, t));
                return (doc: d, score);
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => x.doc)
            .ToList();

        return scored;
    }

    private static int CountOccurrences(string text, string term)
    {
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(term, idx, StringComparison.Ordinal)) >= 0)
        { count++; idx += term.Length; }
        return count;
    }
}
