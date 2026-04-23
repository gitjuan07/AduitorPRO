using AuditorPRO.Domain.Common;
using AuditorPRO.Domain.Enums;

namespace AuditorPRO.Domain.Entities;

public class EmpleadoMaestro : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NumeroEmpleado { get; set; } = string.Empty;
    /// <summary>Cédula de identidad — clave maestra de cruce tridimensional SAP ↔ Nómina ↔ EntraID</summary>
    public string? Cedula { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? CorreoCorporativo { get; set; }
    public string? EntraIdObject { get; set; }
    public int? SociedadId { get; set; }
    public Sociedad? Sociedad { get; set; }
    public int? DepartamentoId { get; set; }
    public Departamento? Departamento { get; set; }
    public int? PuestoId { get; set; }
    public Puesto? Puesto { get; set; }
    public Guid? JefeEmpleadoId { get; set; }
    public EmpleadoMaestro? JefeEmpleado { get; set; }
    public EstadoLaboral EstadoLaboral { get; set; }
    public DateOnly? FechaIngreso { get; set; }
    public DateOnly? FechaBaja { get; set; }
    public string? FuenteOrigen { get; set; }
    public Guid? LoteCargaId { get; set; }
    /// <summary>Cédula normalizada (sin guiones, trim, uppercase) — clave de cruce en controles</summary>
    public string? CedulaNormalizada { get; set; }
}

public class UsuarioSistema : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sistema { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    /// <summary>Cédula de identidad del campo ID del reporte SAP — clave de cruce</summary>
    public string? Cedula { get; set; }
    public string? NombreCompleto { get; set; }
    public string? Sociedad { get; set; }
    public string? Departamento { get; set; }
    public string? Puesto { get; set; }
    public string? Email { get; set; }
    public Guid? EmpleadoId { get; set; }
    public EmpleadoMaestro? Empleado { get; set; }
    public EstadoUsuario Estado { get; set; }
    public string? TipoUsuario { get; set; }
    public DateTime? FechaUltimoAcceso { get; set; }
    public string? FuenteOrigen { get; set; }
    /// <summary>Cédula normalizada (sin guiones, trim, uppercase) — clave de cruce en controles</summary>
    public string? CedulaNormalizada { get; set; }
}

/// <summary>
/// Matriz de Puestos aprobada por Contraloría — estructura idéntica al reporte SAP
/// más la fecha de revisión de Contraloría. Es la referencia oficial de qué roles
/// debería tener cada puesto en la organización.
/// </summary>
public class MatrizPuestoSAP : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Cedula { get; set; }           // ID / Cédula de identidad
    public string UsuarioSAP { get; set; } = string.Empty;
    public string? NombreCompleto { get; set; }
    public string? Sociedad { get; set; }
    public string? Departamento { get; set; }
    public string Puesto { get; set; } = string.Empty;   // Clave de referencia
    public string? Email { get; set; }
    public string Rol { get; set; } = string.Empty;
    public DateOnly? InicioValidez { get; set; }
    public DateOnly? FinValidez { get; set; }
    public string? Transaccion { get; set; }
    public DateTime? UltimoIngreso { get; set; }
    public DateOnly? FechaRevisionContraloria { get; set; }  // Por defecto: 31/07/2025
    public Guid? LoteCargaId { get; set; }
}

/// <summary>
/// Caso del sistema SE Suite que justifica un rol o transacción SAP
/// que excede la Matriz de Puestos. Sin caso vigente = hallazgo automático.
/// </summary>
public class CasoSESuite : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NumeroCaso { get; set; } = string.Empty;   // Clave única
    public string? Titulo { get; set; }
    public string? UsuarioSAP { get; set; }
    public string? Cedula { get; set; }
    public string? RolJustificado { get; set; }
    public string? TransaccionesJustificadas { get; set; }   // Comma-separated
    public DateOnly? FechaAprobacion { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public string EstadoCaso { get; set; } = "APROBADO";
    public string? Aprobador { get; set; }
    public string? ArchivoAdjuntoUrl { get; set; }
    public Guid? LoteCargaId { get; set; }
}

// ─── Snapshots de Entra ID ───────────────────────────────────────────────────

/// <summary>
/// Cabecera de una instantánea de Entra ID (punto en el tiempo).
/// Cada carga genera una nueva cabecera con su propio SnapshotId.
/// </summary>
public class SnapshotEntraID
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Nombre o etiqueta de la instantánea (ej: "Cierre Q1 2025")</summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Fecha y hora exacta en que se tomó la instantánea</summary>
    public DateTime FechaInstantanea { get; set; } = DateTime.UtcNow;
    /// <summary>Total de usuarios en esta instantánea</summary>
    public int TotalRegistros { get; set; }
    public string? CreadoPor { get; set; }
    /// <summary>Origen del snapshot: GRAPH_DIRECT (sincronización automática) o MANUAL_EXCEL (carga por archivo)</summary>
    public string Origen { get; set; } = "MANUAL_EXCEL";
    public ICollection<RegistroEntraID> Registros { get; set; } = [];
}

/// <summary>
/// Fila de usuario dentro de una instantánea de Entra ID.
/// Campos del directorio corporativo + EmployeeId como clave de cruce.
/// </summary>
public class RegistroEntraID
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SnapshotId { get; set; }
    public SnapshotEntraID Snapshot { get; set; } = null!;
    /// <summary>EmployeeId del directorio Entra ID = Cédula de identidad — clave de cruce</summary>
    public string? EmployeeId { get; set; }
    /// <summary>Object ID único en Entra ID (immutableId)</summary>
    public string? ObjectId { get; set; }
    public string? DisplayName { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public bool AccountEnabled { get; set; } = true;
    public string? Manager { get; set; }
    public string? OfficeLocation { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? LastSignInDateTime { get; set; }
    /// <summary>EmployeeId normalizado (sin guiones, trim, uppercase) — clave de cruce con SAP/Nómina</summary>
    public string? EmployeeIdNormalizado { get; set; }
}

/// <summary>
/// Fuente de datos utilizada en una simulación (registro por cada archivo/origen)
/// </summary>
public class FuenteDatoSimulacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SimulacionId { get; set; }
    public string TipoFuente { get; set; } = string.Empty;   // SAP_ROLES | NOMINA | MATRIZ_PUESTOS | CASOS_SESUITE
    public string NombreArchivo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; } = DateTime.UtcNow;
    public int TotalRegistros { get; set; }
    public Guid? LoteCargaId { get; set; }
}
