namespace AuditorPRO.Domain.Entities;

/// <summary>
/// Documento de auditoría indexado para la base de conocimiento (RAG local).
/// Almacena el texto extraído de archivos PDF, Word, Excel, CSV, TXT.
/// </summary>
public class BaseConocimiento
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Origen del documento
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaOriginal  { get; set; } = string.Empty;   // path en servidor o "UPLOAD"
    public string TipoArchivo   { get; set; } = string.Empty;   // PDF, DOCX, XLSX, CSV, TXT
    public long TamanoBytes     { get; set; }

    // Contenido extraído
    public string TextoCompleto { get; set; } = string.Empty;   // texto plano extraído
    public int    TotalPalabras { get; set; }
    public int    TotalPaginas  { get; set; }

    // Clasificación automática (detectada del contenido)
    public string? DominioDetectado    { get; set; }  // ID, CHG, SAP-SEC, etc.
    public string? ControlesDetectados { get; set; }  // JSON array de códigos de control
    public string? Tags                { get; set; }  // JSON array de palabras clave

    // Estado de ingesta
    public string Estado        { get; set; } = "PROCESADO";
    // PROCESADO | ERROR | PENDIENTE
    public string? ErrorDetalle { get; set; }

    // Origen de la ingesta
    public string FuenteIngesta { get; set; } = "DIRECTORIO";
    // DIRECTORIO | UPLOAD | URL

    // Trazabilidad
    public string?   IngresadoPor { get; set; }
    public DateTime  CreadoAt     { get; set; } = DateTime.UtcNow;
    public bool      IsDeleted    { get; set; } = false;
}
