using AuditorPRO.Application.Features.Cargas;
using AuditorPRO.Application.Features.Sociedades;
using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CargasController : ControllerBase
{
    private readonly IMediator _mediator;
    public CargasController(IMediator mediator) => _mediator = mediator;

    // ── Lotes de carga ────────────────────────────────────────────────────────

    [HttpGet("lotes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLotes(
        [FromQuery] string? tipoCarga,
        [FromQuery] string? sociedadCodigo,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetLotesCargaQuery(tipoCarga, sociedadCodigo, limit), ct));

    [HttpGet("lotes/ultimo")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUltimoLote(
        [FromQuery] string tipoCarga,
        [FromQuery] string? sociedadCodigo,
        CancellationToken ct = default)
    {
        var lote = await _mediator.Send(new GetUltimoLoteQuery(tipoCarga, sociedadCodigo), ct);
        return lote is null ? NotFound() : Ok(lote);
    }

    // ─── Helper: resolver nombre de sociedad ─────────────────────────────────
    private async Task<(string? codigo, string? nombre)> ResolveSociedadAsync(string? codigo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return (null, null);
        var soc = await _mediator.Send(new GetSociedadByCodigoQuery(codigo.ToUpperInvariant()), ct);
        return (codigo.ToUpperInvariant(), soc?.Nombre ?? codigo.ToUpperInvariant());
    }

    [HttpGet("plantilla/empleados")]
    [AllowAnonymous]
    public IActionResult PlantillaEmpleados()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Empleados");

        // Encabezados
        // Col 1=NumeroEmpleado, 2=Cedula, 3=Nombre, 4=ApellidoPaterno, 5=ApellidoMaterno,
        // 6=CorreoCorporativo, 7=FechaIngreso, 8=EstadoLaboral, 9=DepartamentoCodigo, 10=PuestoCodigo
        string[] headers = [
            "NumeroEmpleado", "Cedula", "Nombre", "ApellidoPaterno", "ApellidoMaterno",
            "CorreoCorporativo", "FechaIngreso", "EstadoLaboral",
            "DepartamentoCodigo", "PuestoCodigo"
        ];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
            cell.Style.Font.FontColor = XLColor.White;
        }
        // Resaltar columna Cedula en amarillo — es la clave maestra de cruce
        ws.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#b45309");

        // Fila de ejemplo
        ws.Cell(2, 1).Value = "EMP-001";
        ws.Cell(2, 2).Value = "112340567";          // Cedula — clave maestra
        ws.Cell(2, 3).Value = "Juan";
        ws.Cell(2, 4).Value = "Serrano";
        ws.Cell(2, 5).Value = "García";
        ws.Cell(2, 6).Value = "juan.serrano@ilglogistics.com";
        ws.Cell(2, 7).Value = "2024-01-15";
        ws.Cell(2, 8).Value = "ACTIVO";
        ws.Cell(2, 9).Value = "TI";
        ws.Cell(2, 10).Value = "ANALYST";

        // Nota en fila 3
        ws.Cell(3, 1).Value = "* Cedula = clave de cruce SAP ↔ Nómina ↔ Entra ID (OBLIGATORIA para Control Cruzado). EstadoLaboral: ACTIVO | INACTIVO | BAJA_PROCESADA";
        ws.Cell(3, 1).Style.Font.Italic = true;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(3, 1, 3, headers.Length).Merge();

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla_empleados.xlsx");
    }

    [HttpGet("plantilla/usuarios")]
    [AllowAnonymous]
    public IActionResult PlantillaUsuarios()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("UsuariosSistema");

        string[] headers = [
            "Sistema", "NombreUsuario", "NumeroEmpleado",
            "Estado", "TipoUsuario", "FechaUltimoAcceso"
        ];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
            cell.Style.Font.FontColor = XLColor.White;
        }

        ws.Cell(2, 1).Value = "SAP";
        ws.Cell(2, 2).Value = "JSERRANO";
        ws.Cell(2, 3).Value = "EMP-001";
        ws.Cell(2, 4).Value = "ACTIVO";
        ws.Cell(2, 5).Value = "DIALOG";
        ws.Cell(2, 6).Value = "2024-12-01";

        ws.Cell(3, 1).Value = "* Estado: ACTIVO | INACTIVO | BLOQUEADO  |  Sistema: SAP | EVOLUTION | SE_SUITE | AD";
        ws.Cell(3, 1).Style.Font.Italic = true;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(3, 1, 3, headers.Length).Merge();

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla_usuarios.xlsx");
    }

    [HttpPost("empleados")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<IActionResult> CargarEmpleados(
        [FromForm] IFormFile archivo,
        [FromForm] string? sociedadCodigo,
        CancellationToken ct)
    {
        var (cod, nom) = await ResolveSociedadAsync(sociedadCodigo, ct);
        // Resolver ID de sociedad (0 si no se especifica — empleados sin sociedad fija)
        int socId = 0;
        if (!string.IsNullOrWhiteSpace(cod))
        {
            var soc = await _mediator.Send(new GetSociedadByCodigoQuery(cod), ct);
            socId = soc?.Id ?? 0;
        }
        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        ms.Position = 0;
        var cmd = new CargarEmpleadosCommand(ms, archivo.FileName, archivo.ContentType, socId, cod, nom);
        var resultado = await _mediator.Send(cmd, ct);
        return Ok(resultado);
    }

    [HttpPost("usuarios-sistema")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> CargarUsuariosSistema(
        [FromForm] IFormFile archivo,
        [FromForm] string sistema,
        [FromForm] string? sociedadCodigo,
        CancellationToken ct)
    {
        var (cod, nom) = await ResolveSociedadAsync(sociedadCodigo, ct);
        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        ms.Position = 0;
        var cmd = new CargarUsuariosSistemaCommand(ms, archivo.FileName, archivo.ContentType, sistema, cod, nom);
        var resultado = await _mediator.Send(cmd, ct);
        return Ok(resultado);
    }

    [HttpGet("plantilla/sap-roles")]
    [AllowAnonymous]
    public IActionResult PlantillaSapRoles()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("SAP_Roles");

        // Columnas idénticas a la vista V_SAP_USR_RECERTIFICAION
        // Una fila por USUARIO + ROL + TRANSACCION (igual que el export SAP)
        string[] headers = [
            "USUARIO", "NOMBRE_COMPLETO", "SOCIEDAD", "DEPARTAMENTO", "PUESTO",
            "EMAIL", "ROL", "INICIO_VALIDEZ", "FIN_VALIDEZ", "TRANSACCION", "ULTIMO_INGRESO"
        ];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Fila ejemplo — usuario con rol Z_FI_CUENTAS_PAGAR, transacción FB60
        ws.Cell(2, 1).Value  = "JSERRANO";
        ws.Cell(2, 2).Value  = "Juan Serrano García";
        ws.Cell(2, 3).Value  = "ILG-CR";
        ws.Cell(2, 4).Value  = "FINANZAS";
        ws.Cell(2, 5).Value  = "ANALISTA_CONTABLE";
        ws.Cell(2, 6).Value  = "juan.serrano@ilglogistics.com";
        ws.Cell(2, 7).Value  = "Z_FI_CUENTAS_PAGAR";
        ws.Cell(2, 8).Value  = "01/01/2024";
        ws.Cell(2, 9).Value  = "";
        ws.Cell(2, 10).Value = "FB60";
        ws.Cell(2, 11).Value = "15/01/2025";

        // Misma usuario, mismo rol, segunda transacción
        ws.Cell(3, 1).Value  = "JSERRANO";
        ws.Cell(3, 2).Value  = "Juan Serrano García";
        ws.Cell(3, 3).Value  = "ILG-CR";
        ws.Cell(3, 4).Value  = "FINANZAS";
        ws.Cell(3, 5).Value  = "ANALISTA_CONTABLE";
        ws.Cell(3, 6).Value  = "juan.serrano@ilglogistics.com";
        ws.Cell(3, 7).Value  = "Z_FI_CUENTAS_PAGAR";
        ws.Cell(3, 8).Value  = "01/01/2024";
        ws.Cell(3, 9).Value  = "";
        ws.Cell(3, 10).Value = "FB65";
        ws.Cell(3, 11).Value = "15/01/2025";

        // Mismo usuario, segundo rol
        ws.Cell(4, 1).Value  = "JSERRANO";
        ws.Cell(4, 2).Value  = "Juan Serrano García";
        ws.Cell(4, 3).Value  = "ILG-CR";
        ws.Cell(4, 4).Value  = "FINANZAS";
        ws.Cell(4, 5).Value  = "ANALISTA_CONTABLE";
        ws.Cell(4, 6).Value  = "juan.serrano@ilglogistics.com";
        ws.Cell(4, 7).Value  = "Z_MM_COMPRAS";
        ws.Cell(4, 8).Value  = "01/03/2024";
        ws.Cell(4, 9).Value  = "";
        ws.Cell(4, 10).Value = "ME21N";
        ws.Cell(4, 11).Value = "15/01/2025";

        // Nota
        ws.Cell(5, 1).Value = "* Exporta directo desde V_SAP_USR_RECERTIFICAION o usa el mismo formato. Una fila por usuario+rol+transaccion. El sistema agrupa automáticamente.";
        ws.Cell(5, 1).Style.Font.Italic = true;
        ws.Cell(5, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(5, 1, 5, headers.Length).Merge();

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla_sap_roles.xlsx");
    }

    [HttpPost("sap-roles")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    public async Task<IActionResult> CargarRolesSAP(
        [FromForm] IFormFile archivo,
        [FromForm] string sistema,
        [FromForm] string? sociedadCodigo,
        CancellationToken ct)
    {
        var (cod, nom) = await ResolveSociedadAsync(sociedadCodigo, ct);
        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        ms.Position = 0;
        var cmd = new CargarRolesSAPCommand(ms, archivo.FileName, archivo.ContentType, sistema, cod, nom);
        var resultado = await _mediator.Send(cmd, ct);
        return Ok(resultado);
    }

    // ─── Matriz de Puestos (aprobada por Contraloría) ────────────────────────

    [HttpGet("plantilla/matriz-puestos")]
    [AllowAnonymous]
    public IActionResult PlantillaMatrizPuestos()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("MatrizPuestos");

        // Mismas columnas que V_SAP_USR_RECERTIFICAION + FechaRevisionContraloria al final
        string[] headers = [
            "ID", "USUARIO", "NOMBRE_COMPLETO", "SOCIEDAD", "DEPARTAMENTO", "PUESTO",
            "EMAIL", "ROL", "INICIO_VALIDEZ", "FIN_VALIDEZ", "TRANSACCION", "ULTIMO_INGRESO",
            "FECHA_REVISION_CONTRALORIA"
        ];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#7c3aed");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Fila de ejemplo
        ws.Cell(2, 1).Value  = "112340567";
        ws.Cell(2, 2).Value  = "JSERRANO";
        ws.Cell(2, 3).Value  = "Juan Serrano García";
        ws.Cell(2, 4).Value  = "ILG-CR";
        ws.Cell(2, 5).Value  = "FINANZAS";
        ws.Cell(2, 6).Value  = "ANALISTA_CONTABLE";
        ws.Cell(2, 7).Value  = "juan.serrano@ilglogistics.com";
        ws.Cell(2, 8).Value  = "Z_FI_CUENTAS_PAGAR";
        ws.Cell(2, 9).Value  = "01/01/2024";
        ws.Cell(2, 10).Value = "";
        ws.Cell(2, 11).Value = "FB60";
        ws.Cell(2, 12).Value = "15/01/2025";
        ws.Cell(2, 13).Value = "31/07/2025";

        // Nota
        ws.Cell(3, 1).Value = "* Misma estructura que V_SAP_USR_RECERTIFICAION + columna FECHA_REVISION_CONTRALORIA (default 31/07/2025 si se omite).";
        ws.Cell(3, 1).Style.Font.Italic = true;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(3, 1, 3, headers.Length).Merge();

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla_matriz_puestos.xlsx");
    }

    [HttpGet("matriz-puestos")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMatrizPuestos(
        [FromQuery] string? usuario,
        [FromQuery] string? puesto,
        [FromQuery] string? rol,
        [FromQuery] string? transaccion,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetMatrizPuestosQuery(usuario, puesto, rol, transaccion, page, pageSize);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPost("matriz-puestos")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    public async Task<IActionResult> CargarMatrizPuestos(
        [FromForm] IFormFile archivo,
        [FromForm] string? sociedadCodigo,
        CancellationToken ct)
    {
        var (cod, nom) = await ResolveSociedadAsync(sociedadCodigo, ct);
        var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        ms.Position = 0;
        var cmd = new CargarMatrizPuestosCommand(ms, archivo.FileName, archivo.ContentType, cod, nom);
        var resultado = await _mediator.Send(cmd, ct);
        return Ok(resultado);
    }

    // ─── Casos SE Suite ───────────────────────────────────────────────────────

    [HttpGet("plantilla/casos-sesuite")]
    [AllowAnonymous]
    public IActionResult PlantillaCasosSESuite()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("CasosSESuite");

        string[] headers = [
            "NUMERO_CASO", "TITULO", "USUARIO_SAP", "CEDULA",
            "ROL_JUSTIFICADO", "TRANSACCIONES", "FECHA_APROBACION",
            "FECHA_VENCIMIENTO", "ESTADO", "APROBADOR"
        ];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#b45309");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Fila de ejemplo
        ws.Cell(2, 1).Value  = "CASE-2024-001";
        ws.Cell(2, 2).Value  = "Acceso contabilidad especial — Proyecto cierre Q4";
        ws.Cell(2, 3).Value  = "JSERRANO";
        ws.Cell(2, 4).Value  = "112340567";
        ws.Cell(2, 5).Value  = "Z_FI_CIERRE_ANUAL";
        ws.Cell(2, 6).Value  = "F.01,FAGLB03,S_ALR_87012284";
        ws.Cell(2, 7).Value  = "15/11/2024";
        ws.Cell(2, 8).Value  = "31/01/2025";
        ws.Cell(2, 9).Value  = "APROBADO";
        ws.Cell(2, 10).Value = "María Contreras (Contraloría)";

        // Nota
        ws.Cell(3, 1).Value = "* ESTADO: APROBADO | VENCIDO | ANULADO  |  TRANSACCIONES: separadas por coma";
        ws.Cell(3, 1).Style.Font.Italic = true;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(3, 1, 3, headers.Length).Merge();

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla_casos_sesuite.xlsx");
    }

    [HttpPost("casos-sesuite")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    public async Task<IActionResult> CargarCasosSESuite(
        [FromForm] IFormFile archivo,
        [FromForm] string? sociedadCodigo,
        CancellationToken ct)
    {
        var (cod, nom) = await ResolveSociedadAsync(sociedadCodigo, ct);
        var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        ms.Position = 0;
        var cmd = new CargarCasosSESuiteCommand(ms, archivo.FileName, archivo.ContentType, cod, nom);
        var resultado = await _mediator.Send(cmd, ct);
        return Ok(resultado);
    }

    // ─── Snapshots de Usuarios Entra ID ──────────────────────────────────────

    [HttpGet("plantilla/snapshot-entraid")]
    [AllowAnonymous]
    public IActionResult PlantillaSnapshotEntraID()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("EntraID");

        string[] headers = [
            "EmployeeId", "ObjectId", "DisplayName", "UserPrincipalName", "Email",
            "Department", "JobTitle", "AccountEnabled", "Manager", "OfficeLocation",
            "CreatedDateTime", "LastSignInDateTime"
        ];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Fila de ejemplo
        ws.Cell(2, 1).Value  = "112340567";        // EmployeeId = Cédula
        ws.Cell(2, 2).Value  = "a1b2c3d4-...";     // ObjectId Entra ID
        ws.Cell(2, 3).Value  = "Juan Serrano García";
        ws.Cell(2, 4).Value  = "juan.serrano@ilglogistics.com";
        ws.Cell(2, 5).Value  = "juan.serrano@ilglogistics.com";
        ws.Cell(2, 6).Value  = "FINANZAS";
        ws.Cell(2, 7).Value  = "ANALISTA_CONTABLE";
        ws.Cell(2, 8).Value  = "TRUE";
        ws.Cell(2, 9).Value  = "María Contreras";
        ws.Cell(2, 10).Value = "San José, CR";
        ws.Cell(2, 11).Value = "15/01/2023 08:00";
        ws.Cell(2, 12).Value = "10/04/2026 09:35";

        ws.Cell(3, 1).Value = "* EmployeeId = Cédula de identidad (campo clave de cruce). " +
            "AccountEnabled: TRUE/FALSE. Exporta desde Azure AD o Graph API. " +
            "Cada carga genera una nueva instantánea (snapshot) independiente.";
        ws.Cell(3, 1).Style.Font.Italic = true;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(3, 1, 3, headers.Length).Merge();

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla_entra_id.xlsx");
    }

    [HttpPost("snapshot-entraid")]
    [RequestSizeLimit(20_971_520)]
    public async Task<IActionResult> CargarSnapshotEntraID(
        [FromForm] IFormFile archivo,
        [FromForm] string? nombre,
        CancellationToken ct)
    {
        var cmd = new CargarSnapshotEntraIDCommand(
            archivo.OpenReadStream(),
            archivo.FileName,
            archivo.ContentType,
            nombre
        );
        var resultado = await _mediator.Send(cmd, ct);
        return Ok(resultado);
    }

    [HttpGet("snapshots-entraid")]
    public async Task<IActionResult> GetSnapshotsEntraID(CancellationToken ct)
    {
        var resultado = await _mediator.Send(new GetSnapshotsEntraIDQuery(), ct);
        return Ok(resultado);
    }

    [HttpGet("snapshot-entraid/{id:guid}/excel")]
    public async Task<IActionResult> DescargarSnapshotEntraID(Guid id, CancellationToken ct)
    {
        var resultado = await _mediator.Send(new DescargarSnapshotEntraIDQuery(id), ct);
        return File(resultado.Contenido,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            resultado.NombreArchivo);
    }

    // ─── Sincronización directa Entra ID via Microsoft Graph ─────────────────

    /// <summary>
    /// Sincroniza el directorio Entra ID directamente desde Microsoft Graph.
    /// Requiere que el backend tenga permisos User.Read.All (y AuditLog.Read.All para signInActivity).
    /// Fallback: usar POST /api/cargas/snapshot-entraid con archivo Excel.
    /// </summary>
    [HttpPost("snapshot-entraid/sync")]
    public async Task<IActionResult> SyncEntraIDDirecto(
        [FromBody] SyncEntraIDRequest? request,
        CancellationToken ct)
    {
        var cmd = new SyncEntraIDDirectoCommand(request?.NombreInstantanea);
        var resultado = await _mediator.Send(cmd, ct);

        if (resultado.SnapshotId == Guid.Empty)
        {
            return StatusCode(502, new
            {
                mensaje = "No se pudo sincronizar con Microsoft Graph.",
                errores = resultado.DetalleErrores
            });
        }

        return Ok(resultado);
    }

    [HttpPost("purgar-antiguas")]
    [ProducesResponseType(typeof(PurgarCargasResultado), 200)]
    public async Task<IActionResult> PurgarCargasAntiguas(CancellationToken ct)
    {
        var resultado = await _mediator.Send(new PurgarCargasAntiguasCommand(), ct);
        return Ok(resultado);
    }
}

public record SyncEntraIDRequest(string? NombreInstantanea);
