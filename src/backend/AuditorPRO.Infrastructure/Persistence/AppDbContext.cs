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
    public DbSet<MatrizPuestoSAP> MatrizPuestosSAP => Set<MatrizPuestoSAP>();
    public DbSet<CasoSESuite> CasosSESuite => Set<CasoSESuite>();
    public DbSet<FuenteDatoSimulacion> FuentesDatosSimulacion => Set<FuenteDatoSimulacion>();
    public DbSet<BaseConocimiento> BaseConocimiento => Set<BaseConocimiento>();
    public DbSet<SnapshotEntraID> SnapshotsEntraID => Set<SnapshotEntraID>();
    public DbSet<RegistroEntraID> RegistrosEntraID => Set<RegistroEntraID>();
    public DbSet<LoteCarga> LotesCarga => Set<LoteCarga>();

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
            e.Property(x => x.EvidenciaGenerada).HasDefaultValue(false);
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

        modelBuilder.Entity<UsuarioSistema>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Sistema, x.NombreUsuario });
            e.HasIndex(x => new { x.Sistema, x.Cedula });
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<MatrizPuestoSAP>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Puesto, x.Rol });
            e.HasIndex(x => x.Cedula);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<CasoSESuite>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NumeroCaso).IsUnique();
            e.HasIndex(x => new { x.UsuarioSAP, x.RolJustificado });
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<SnapshotEntraID>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.FechaInstantanea);
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<RegistroEntraID>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SnapshotId, x.EmployeeId });
            e.HasIndex(x => x.EmployeeId);
            e.HasOne(r => r.Snapshot)
             .WithMany(s => s.Registros)
             .HasForeignKey(r => r.SnapshotId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoteCarga>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TipoCarga, x.SociedadCodigo, x.FechaCarga });
            e.HasIndex(x => new { x.TipoCarga, x.EsVigente });
            e.Property(x => x.TipoCarga).HasMaxLength(30).IsRequired();
            e.Property(x => x.SociedadCodigo).HasMaxLength(10);
            e.Property(x => x.SociedadNombre).HasMaxLength(200);
        });

        modelBuilder.Entity<FuenteDatoSimulacion>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SimulacionId);
            e.HasOne<SimulacionAuditoria>()
             .WithMany(s => s.FuentesDatos)
             .HasForeignKey(f => f.SimulacionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
