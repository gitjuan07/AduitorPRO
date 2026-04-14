namespace AuditorPRO.Domain.Entities;

/// <summary>
/// Registro de cada carga masiva ejecutada.
/// Todos los registros cargados referencian su LoteCarga para trazabilidad completa.
/// Las simulaciones usan los últimos lotes activos de cada tipo como referencia de datos.
/// </summary>
public class LoteCarga
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>SAP_ROLES | MATRIZ_PUESTOS | EMPLEADOS | CASOS_SESUITE | ENTRA_ID</summary>
    public string TipoCarga { get; set; } = string.Empty;

    public DateTime FechaCarga { get; set; } = DateTime.UtcNow;

    /// <summary>Código SAP de la sociedad (ej: CR01, PA03)</summary>
    public string? SociedadCodigo { get; set; }

    /// <summary>Nombre resuelto de la sociedad al momento de la carga</summary>
    public string? SociedadNombre { get; set; }

    public string? NombreArchivo { get; set; }
    public int TotalRegistros { get; set; }
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public int Errores { get; set; }
    public string? CargadoPor { get; set; }

    /// <summary>true = vigente para simulaciones; false = reemplazado por carga más reciente del mismo tipo+sociedad</summary>
    public bool EsVigente { get; set; } = true;
}
