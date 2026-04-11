using AuditorPRO.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditorPRO.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Sociedad> Sociedades => Set<Sociedad>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Puesto> Puestos => Set<Puesto>();
    public DbSet<EmpleadoMaestro> Empleados => Set<EmpleadoMaestro>();
    public DbSet<UsuarioSistema> UsuariosSistema => Set<UsuarioSistema>();
    public DbSet<DominioAuditoria> Dominios => Set<DominioAuditoria>();
    public DbSet<PuntoControl> PuntosControl => Set<PuntoControl>();
    public DbSet<SimulacionAuditoria> Simulaciones => Set<SimulacionAuditoria>();
    public DbSet<ResultadoControl> ResultadosControl => Set<ResultadoControl>();
    public DbSet<Hallazgo> Hallazgos => Set<Hallazgo>();
    public DbSet<Evidencia> Evidencias => Set<Evidencia>();
    public DbSet<Conector> Conectores => Set<Conector>();
    public DbSet<LogConector> LogsConector => Set<LogConector>();
    public DbSet<Politica> Politicas => Set<Politica>();
    public DbSet<BitacoraEvento> Bitacora => Set<BitacoraEvento>();
    public DbSet<RolSistema> RolesSistema => Set<RolSistema>();
    public DbSet<AsignacionRolUsuario> AsignacionesRol => Set<AsignacionRolUsuario>();
    public DbSet<ConflictoSoD> ConflictosSoD => Set<ConflictoSoD>();
    public DbSet<MatrizPuestoRol> MatrizPuestoRol => Set<MatrizPuestoRol>();
    public DbSet<BaseConocimiento> BaseConocimiento => Set<BaseConocimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Sociedad>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(10).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        modelBuilder.Entity<EmpleadoMaestro>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NumeroEmpleado).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.NumeroEmpleado).IsUnique();
            e.HasIndex(x => new { x.EstadoLaboral, x.SociedadId });
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<SimulacionAuditoria>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Estado, x.IniciadaAt });
            e.HasQueryFilter(x => !x.IsDeleted);
            e.Property(x => x.ScoreMadurez).HasPrecision(4, 2);
            e.Property(x => x.PorcentajeCumplimiento).HasPrecision(5, 2);
        });

        modelBuilder.Entity<ResultadoControl>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SimulacionId, x.Semaforo });
        });

        modelBuilder.Entity<Hallazgo>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<BitacoraEvento>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UsuarioId, x.OcurridoAt });
            e.HasIndex(x => x.OcurridoAt);
        });

        modelBuilder.Entity<Conector>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
        });
    }
}
