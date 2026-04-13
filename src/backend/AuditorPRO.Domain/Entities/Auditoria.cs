using AuditorPRO.Domain.Common;
using AuditorPRO.Domain.Enums;

namespace AuditorPRO.Domain.Entities;

public class DominioAuditoria
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<PuntoControl> PuntosControl { get; set; } = [];
}

public class PuntoControl
{
    public int Id { get; set; }
    public int DominioId { get; set; }
    public DominioAuditoria Dominio { get; set; } = null!;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoEvaluacion TipoEvaluacion { get; set; }
    public Criticidad CriticidadBase { get; set; }
    public string? NormaReferencia { get; set; }
    public string? QuerySql { get; set; }
    public string? CondicionVerde { get; set; }
    public string? CondicionAmarillo { get; set; }
    public string? CondicionRojo { get; set; }
    public string? EvidenciaRequerida { get; set; }
    public bool Activo { get; set; } = true;
    public int VersionRegla { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SimulacionAuditoria : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoSimulacion Tipo { get; set; }
    public PeriodicidadSimulacion? Periodicidad { get; set; }
    public EstadoSimulacion Estado { get; set; } = EstadoSimulacion.PENDIENTE;
    public string? SociedadIds { get; set; }
    public DateOnly PeriodoInicio { get; set; }
    public DateOnly PeriodoFin { get; set; }
    public string? DominioIds { get; set; }
    public string? PuntosControlIds { get; set; }
    public decimal? ScoreMadurez { get; set; }
    public decimal? PorcentajeCumplimiento { get; set; }
    public int? TotalControles { get; set; }
    public int? ControlesVerde { get; set; }
    public int? ControlesAmarillo { get; set; }
    public int? ControlesRojo { get; set; }
    public string IniciadaPor { get; set; } = string.Empty;
    public DateTime IniciadaAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletadaAt { get; set; }
    public int? DuracionSegundos { get; set; }
    public string? ErrorDetalle { get; set; }
    /// <summary>Objetivo o comentario libre del auditor para esta simulación</summary>
    public string? Objetivo { get; set; }
    /// <summary>Tipo de simulación de control cruzado: COMPLETO | SAP_NOMINA | SAP_ENTRA_ID | SOD</summary>
    public string? TipoSimulacion { get; set; }
    /// <summary>Resumen JSON de resultados del motor de control cruzado</summary>
    public string? ResumenResultados { get; set; }
    /// <summary>Total de hallazgos marcados como CRITICO generados por esta simulación</summary>
    public int TotalCriticos { get; set; }
    public ICollection<ResultadoControl> Resultados { get; set; } = [];
    public ICollection<FuenteDatoSimulacion> FuentesDatos { get; set; } = [];
}

public class ResultadoControl
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SimulacionId { get; set; }
    public SimulacionAuditoria Simulacion { get; set; } = null!;
    public int PuntoControlId { get; set; }
    public PuntoControl PuntoControl { get; set; } = null!;
    public int? SociedadId { get; set; }
    public Sociedad? Sociedad { get; set; }
    public SemaforoColor Semaforo { get; set; }
    public Criticidad Criticidad { get; set; }
    public string? ResultadoDetalle { get; set; }
    public string? DatosEvaluados { get; set; }
    public string? EvidenciaEncontrada { get; set; }
    public string? EvidenciaFaltante { get; set; }
    public string? AnalisisIa { get; set; }
    public string? Recomendacion { get; set; }
    public string? ResponsableSugerido { get; set; }
    public DateOnly? FechaCompromisoSug { get; set; }
    public DateTime EvaluadoAt { get; set; } = DateTime.UtcNow;
}
