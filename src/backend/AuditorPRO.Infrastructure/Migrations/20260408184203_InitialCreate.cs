using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorPRO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bitacora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UsuarioEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Accion = table.Column<int>(type: "int", nullable: false),
                    Recurso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecursoId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosAntes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosDespues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exitoso = table.Column<bool>(type: "bit", nullable: false),
                    ErrorDetalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OcurridoAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bitacora", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conectores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoConector = table.Column<int>(type: "int", nullable: false),
                    Sistema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    ConfiguracionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UrlEndpoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecretKeyVaultRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimaEjecucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimaEjecucionExito = table.Column<bool>(type: "bit", nullable: false),
                    UltimoError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalEjecuciones = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conectores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dominios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dominios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Politicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    NormaReferencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Responsable = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaVigencia = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaRevision = table.Column<DateOnly>(type: "date", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    DocumentoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Politicas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolesSistema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sistema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreRol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NivelRiesgo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EsCritico = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Simulaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Periodicidad = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    SociedadIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PeriodoInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodoFin = table.Column<DateOnly>(type: "date", nullable: false),
                    DominioIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PuntosControlIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScoreMadurez = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    PorcentajeCumplimiento = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TotalControles = table.Column<int>(type: "int", nullable: true),
                    ControlesVerde = table.Column<int>(type: "int", nullable: true),
                    ControlesAmarillo = table.Column<int>(type: "int", nullable: true),
                    ControlesRojo = table.Column<int>(type: "int", nullable: true),
                    IniciadaPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IniciadaAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletadaAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DuracionSegundos = table.Column<int>(type: "int", nullable: true),
                    ErrorDetalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sociedades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Pais = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sociedades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogsConector",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exitoso = table.Column<bool>(type: "bit", nullable: false),
                    RegistrosProcesados = table.Column<int>(type: "int", nullable: true),
                    MensajeError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DuracionMs = table.Column<int>(type: "int", nullable: false),
                    EjecutadoAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EjecutadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsConector", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsConector_Conectores_ConectorId",
                        column: x => x.ConectorId,
                        principalTable: "Conectores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PuntosControl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DominioId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoEvaluacion = table.Column<int>(type: "int", nullable: false),
                    CriticidadBase = table.Column<int>(type: "int", nullable: false),
                    NormaReferencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuerySql = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CondicionVerde = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CondicionAmarillo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CondicionRojo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenciaRequerida = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    VersionRegla = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuntosControl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuntosControl_Dominios_DominioId",
                        column: x => x.DominioId,
                        principalTable: "Dominios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConflictosSoD",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sistema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RolAId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RolBId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Riesgo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MitigacionDoc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictosSoD", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConflictosSoD_RolesSistema_RolAId",
                        column: x => x.RolAId,
                        principalTable: "RolesSistema",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConflictosSoD_RolesSistema_RolBId",
                        column: x => x.RolBId,
                        principalTable: "RolesSistema",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Departamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SociedadId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departamentos_Sociedades_SociedadId",
                        column: x => x.SociedadId,
                        principalTable: "Sociedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Puestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SociedadId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NivelRiesgo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puestos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Puestos_Sociedades_SociedadId",
                        column: x => x.SociedadId,
                        principalTable: "Sociedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResultadosControl",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SimulacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PuntoControlId = table.Column<int>(type: "int", nullable: false),
                    SociedadId = table.Column<int>(type: "int", nullable: true),
                    Semaforo = table.Column<int>(type: "int", nullable: false),
                    Criticidad = table.Column<int>(type: "int", nullable: false),
                    ResultadoDetalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosEvaluados = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenciaEncontrada = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenciaFaltante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalisisIa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recomendacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsableSugerido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCompromisoSug = table.Column<DateOnly>(type: "date", nullable: true),
                    EvaluadoAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadosControl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultadosControl_PuntosControl_PuntoControlId",
                        column: x => x.PuntoControlId,
                        principalTable: "PuntosControl",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultadosControl_Simulaciones_SimulacionId",
                        column: x => x.SimulacionId,
                        principalTable: "Simulaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultadosControl_Sociedades_SociedadId",
                        column: x => x.SociedadId,
                        principalTable: "Sociedades",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroEmpleado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorreoCorporativo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntraIdObject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SociedadId = table.Column<int>(type: "int", nullable: true),
                    DepartamentoId = table.Column<int>(type: "int", nullable: true),
                    PuestoId = table.Column<int>(type: "int", nullable: true),
                    JefeEmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstadoLaboral = table.Column<int>(type: "int", nullable: false),
                    FechaIngreso = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaBaja = table.Column<DateOnly>(type: "date", nullable: true),
                    FuenteOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoteCargaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empleados_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Empleados_Empleados_JefeEmpleadoId",
                        column: x => x.JefeEmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Empleados_Puestos_PuestoId",
                        column: x => x.PuestoId,
                        principalTable: "Puestos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Empleados_Sociedades_SociedadId",
                        column: x => x.SociedadId,
                        principalTable: "Sociedades",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MatrizPuestoRol",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PuestoId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Justificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VigenteDesdE = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatrizPuestoRol", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatrizPuestoRol_Puestos_PuestoId",
                        column: x => x.PuestoId,
                        principalTable: "Puestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatrizPuestoRol_RolesSistema_RolId",
                        column: x => x.RolId,
                        principalTable: "RolesSistema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hallazgos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SimulacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultadoControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SociedadId = table.Column<int>(type: "int", nullable: true),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Criticidad = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    NormaAfectada = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiesgoAsociado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsableEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCompromiso = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaCierre = table.Column<DateOnly>(type: "date", nullable: true),
                    PlanAccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalisisIa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenciaCierreIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hallazgos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hallazgos_ResultadosControl_ResultadoControlId",
                        column: x => x.ResultadoControlId,
                        principalTable: "ResultadosControl",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Hallazgos_Simulaciones_SimulacionId",
                        column: x => x.SimulacionId,
                        principalTable: "Simulaciones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Hallazgos_Sociedades_SociedadId",
                        column: x => x.SociedadId,
                        principalTable: "Sociedades",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UsuariosSistema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sistema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreUsuario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    TipoUsuario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaUltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FuenteOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosSistema", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosSistema_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Evidencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HallazgoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SimulacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoEvidencia = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescripcionArchivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlobUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlobContainer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubidoPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubidoAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Verificada = table.Column<bool>(type: "bit", nullable: false),
                    HashSha256 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evidencias_Hallazgos_HallazgoId",
                        column: x => x.HallazgoId,
                        principalTable: "Hallazgos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesRol",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaAsignacion = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    AsignadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpedienteRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesRol", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesRol_RolesSistema_RolId",
                        column: x => x.RolId,
                        principalTable: "RolesSistema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsignacionesRol_UsuariosSistema_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "UsuariosSistema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesRol_RolId",
                table: "AsignacionesRol",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesRol_UsuarioId",
                table: "AsignacionesRol",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Bitacora_OcurridoAt",
                table: "Bitacora",
                column: "OcurridoAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bitacora_UsuarioId_OcurridoAt",
                table: "Bitacora",
                columns: new[] { "UsuarioId", "OcurridoAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConflictosSoD_RolAId",
                table: "ConflictosSoD",
                column: "RolAId");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictosSoD_RolBId",
                table: "ConflictosSoD",
                column: "RolBId");

            migrationBuilder.CreateIndex(
                name: "IX_Departamentos_SociedadId",
                table: "Departamentos",
                column: "SociedadId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_DepartamentoId",
                table: "Empleados",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_EstadoLaboral_SociedadId",
                table: "Empleados",
                columns: new[] { "EstadoLaboral", "SociedadId" });

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_JefeEmpleadoId",
                table: "Empleados",
                column: "JefeEmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_NumeroEmpleado",
                table: "Empleados",
                column: "NumeroEmpleado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_PuestoId",
                table: "Empleados",
                column: "PuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_SociedadId",
                table: "Empleados",
                column: "SociedadId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_HallazgoId",
                table: "Evidencias",
                column: "HallazgoId");

            migrationBuilder.CreateIndex(
                name: "IX_Hallazgos_ResultadoControlId",
                table: "Hallazgos",
                column: "ResultadoControlId");

            migrationBuilder.CreateIndex(
                name: "IX_Hallazgos_SimulacionId",
                table: "Hallazgos",
                column: "SimulacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Hallazgos_SociedadId",
                table: "Hallazgos",
                column: "SociedadId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsConector_ConectorId",
                table: "LogsConector",
                column: "ConectorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatrizPuestoRol_PuestoId",
                table: "MatrizPuestoRol",
                column: "PuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_MatrizPuestoRol_RolId",
                table: "MatrizPuestoRol",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Puestos_SociedadId",
                table: "Puestos",
                column: "SociedadId");

            migrationBuilder.CreateIndex(
                name: "IX_PuntosControl_DominioId",
                table: "PuntosControl",
                column: "DominioId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosControl_PuntoControlId",
                table: "ResultadosControl",
                column: "PuntoControlId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosControl_SimulacionId_Semaforo",
                table: "ResultadosControl",
                columns: new[] { "SimulacionId", "Semaforo" });

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosControl_SociedadId",
                table: "ResultadosControl",
                column: "SociedadId");

            migrationBuilder.CreateIndex(
                name: "IX_Simulaciones_Estado_IniciadaAt",
                table: "Simulaciones",
                columns: new[] { "Estado", "IniciadaAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sociedades_Codigo",
                table: "Sociedades",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSistema_EmpleadoId",
                table: "UsuariosSistema",
                column: "EmpleadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesRol");

            migrationBuilder.DropTable(
                name: "Bitacora");

            migrationBuilder.DropTable(
                name: "ConflictosSoD");

            migrationBuilder.DropTable(
                name: "Evidencias");

            migrationBuilder.DropTable(
                name: "LogsConector");

            migrationBuilder.DropTable(
                name: "MatrizPuestoRol");

            migrationBuilder.DropTable(
                name: "Politicas");

            migrationBuilder.DropTable(
                name: "UsuariosSistema");

            migrationBuilder.DropTable(
                name: "Hallazgos");

            migrationBuilder.DropTable(
                name: "Conectores");

            migrationBuilder.DropTable(
                name: "RolesSistema");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "ResultadosControl");

            migrationBuilder.DropTable(
                name: "Departamentos");

            migrationBuilder.DropTable(
                name: "Puestos");

            migrationBuilder.DropTable(
                name: "PuntosControl");

            migrationBuilder.DropTable(
                name: "Simulaciones");

            migrationBuilder.DropTable(
                name: "Sociedades");

            migrationBuilder.DropTable(
                name: "Dominios");
        }
    }
}
