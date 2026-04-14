using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorPRO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3_LoteCarga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaReferenciaDatos",
                table: "Simulaciones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotesUsadosJson",
                table: "Simulaciones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Objetivo",
                table: "Simulaciones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumenResultados",
                table: "Simulaciones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoSimulacion",
                table: "Simulaciones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalCriticos",
                table: "Simulaciones",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CasoSESuiteRef",
                table: "Hallazgos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cedula",
                table: "Hallazgos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EvidenciaGenerada",
                table: "Hallazgos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RolAfectado",
                table: "Hallazgos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoHallazgo",
                table: "Hallazgos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransaccionesAfectadas",
                table: "Hallazgos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioSAP",
                table: "Hallazgos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LotesCarga",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipoCarga = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SociedadCodigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    SociedadNombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NombreArchivo = table.Column<string>(type: "TEXT", nullable: true),
                    TotalRegistros = table.Column<int>(type: "INTEGER", nullable: false),
                    Insertados = table.Column<int>(type: "INTEGER", nullable: false),
                    Actualizados = table.Column<int>(type: "INTEGER", nullable: false),
                    Errores = table.Column<int>(type: "INTEGER", nullable: false),
                    CargadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    EsVigente = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesCarga", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotsEntraID",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FechaInstantanea = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalRegistros = table.Column<int>(type: "INTEGER", nullable: false),
                    CreadoPor = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotsEntraID", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosEntraID",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", nullable: true),
                    ObjectId = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserPrincipalName = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Department = table.Column<string>(type: "TEXT", nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", nullable: true),
                    AccountEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Manager = table.Column<string>(type: "TEXT", nullable: true),
                    OfficeLocation = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSignInDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosEntraID", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosEntraID_SnapshotsEntraID_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "SnapshotsEntraID",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LotesCarga_TipoCarga_EsVigente",
                table: "LotesCarga",
                columns: new[] { "TipoCarga", "EsVigente" });

            migrationBuilder.CreateIndex(
                name: "IX_LotesCarga_TipoCarga_SociedadCodigo_FechaCarga",
                table: "LotesCarga",
                columns: new[] { "TipoCarga", "SociedadCodigo", "FechaCarga" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosEntraID_EmployeeId",
                table: "RegistrosEntraID",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosEntraID_SnapshotId_EmployeeId",
                table: "RegistrosEntraID",
                columns: new[] { "SnapshotId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotsEntraID_FechaInstantanea",
                table: "SnapshotsEntraID",
                column: "FechaInstantanea");

            migrationBuilder.AddForeignKey(
                name: "FK_FuentesDatosSimulacion_Simulaciones_SimulacionId",
                table: "FuentesDatosSimulacion",
                column: "SimulacionId",
                principalTable: "Simulaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuentesDatosSimulacion_Simulaciones_SimulacionId",
                table: "FuentesDatosSimulacion");

            migrationBuilder.DropTable(
                name: "LotesCarga");

            migrationBuilder.DropTable(
                name: "RegistrosEntraID");

            migrationBuilder.DropTable(
                name: "SnapshotsEntraID");

            migrationBuilder.DropColumn(
                name: "FechaReferenciaDatos",
                table: "Simulaciones");

            migrationBuilder.DropColumn(
                name: "LotesUsadosJson",
                table: "Simulaciones");

            migrationBuilder.DropColumn(
                name: "Objetivo",
                table: "Simulaciones");

            migrationBuilder.DropColumn(
                name: "ResumenResultados",
                table: "Simulaciones");

            migrationBuilder.DropColumn(
                name: "TipoSimulacion",
                table: "Simulaciones");

            migrationBuilder.DropColumn(
                name: "TotalCriticos",
                table: "Simulaciones");

            migrationBuilder.DropColumn(
                name: "CasoSESuiteRef",
                table: "Hallazgos");

            migrationBuilder.DropColumn(
                name: "Cedula",
                table: "Hallazgos");

            migrationBuilder.DropColumn(
                name: "EvidenciaGenerada",
                table: "Hallazgos");

            migrationBuilder.DropColumn(
                name: "RolAfectado",
                table: "Hallazgos");

            migrationBuilder.DropColumn(
                name: "TipoHallazgo",
                table: "Hallazgos");

            migrationBuilder.DropColumn(
                name: "TransaccionesAfectadas",
                table: "Hallazgos");

            migrationBuilder.DropColumn(
                name: "UsuarioSAP",
                table: "Hallazgos");
        }
    }
}
