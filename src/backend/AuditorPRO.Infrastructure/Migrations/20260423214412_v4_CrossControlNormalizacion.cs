using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorPRO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v4_CrossControlNormalizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CedulaNormalizada",
                table: "UsuariosSistema",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origen",
                table: "SnapshotsEntraID",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "MANUAL_EXCEL");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeIdNormalizado",
                table: "RegistrosEntraID",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CedulaNormalizada",
                table: "Empleados",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CedulaNormalizada",
                table: "UsuariosSistema");

            migrationBuilder.DropColumn(
                name: "Origen",
                table: "SnapshotsEntraID");

            migrationBuilder.DropColumn(
                name: "EmployeeIdNormalizado",
                table: "RegistrosEntraID");

            migrationBuilder.DropColumn(
                name: "CedulaNormalizada",
                table: "Empleados");
        }
    }
}
