namespace AuditorPRO.Domain.Entities;

public class RolSistema
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sistema { get; set; } = string.Empty;
    public string NombreRol { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? NivelRiesgo { get; set; }
    public string? TransaccionesAutorizadas { get; set; }  // Comma-separated tcodes
    public bool EsCritico { get; set; } = false;
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AsignacionRolUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public UsuarioSistema Usuario { get; set; } = null!;
    public Guid RolId { get; set; }
    public RolSistema Rol { get; set; } = null!;
    public DateOnly? FechaAsignacion { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public string? AsignadoPor { get; set; }
    public string? ExpedienteRef { get; set; }
    /// <summary>Número de caso SE Suite que justifica este acceso (si supera la Matriz de Puestos)</summary>
    public string? CasoSESuiteRef { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ConflictoSoD
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Sistema { get; set; }
    public Guid? RolAId { get; set; }
    public RolSistema? RolA { get; set; }
    public Guid? RolBId { get; set; }
    public RolSistema? RolB { get; set; }
    public string? Descripcion { get; set; }
    public string? Riesgo { get; set; }
    public string? MitigacionDoc { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MatrizPuestoRol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int PuestoId { get; set; }
    public Puesto Puesto { get; set; } = null!;
    public Guid RolId { get; set; }
    public RolSistema Rol { get; set; } = null!;
    public string? Tipo { get; set; }
    public string? Justificacion { get; set; }
    public DateOnly? VigenteDesdE { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
