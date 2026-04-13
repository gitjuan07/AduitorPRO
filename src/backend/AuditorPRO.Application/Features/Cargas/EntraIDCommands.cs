using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Interfaces;
using ClosedXML.Excel;
using MediatR;

namespace AuditorPRO.Application.Features.Cargas;

// ─── Cargar Snapshot Entra ID ────────────────────────────────────────────────

public record CargarSnapshotEntraIDCommand(
    Stream Contenido,
    string NombreArchivo,
    string ContentType,
    /// <summary>Etiqueta libre del auditor, p.ej. "Cierre Q1 2026"</summary>
    string? NombreInstantanea = null
) : IRequest<CargarSnapshotEntraIDResultado>;

public record CargarSnapshotEntraIDResultado(
    Guid SnapshotId,
    string Nombre,
    DateTime FechaInstantanea,
    int TotalRegistros,
    int Errores,
    List<string> DetalleErrores
);

public class CargarSnapshotEntraIDHandler : IRequestHandler<CargarSnapshotEntraIDCommand, CargarSnapshotEntraIDResultado>
{
    private readonly IRepository<SnapshotEntraID> _snapshotRepo;
    private readonly IRepository<RegistroEntraID> _registroRepo;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;

    public CargarSnapshotEntraIDHandler(
        IRepository<SnapshotEntraID> snapshotRepo,
        IRepository<RegistroEntraID> registroRepo,
        ICurrentUserService user,
        IAuditLoggerService audit)
    { _snapshotRepo = snapshotRepo; _registroRepo = registroRepo; _user = user; _audit = audit; }

    public async Task<CargarSnapshotEntraIDResultado> Handle(
        CargarSnapshotEntraIDCommand request, CancellationToken ct)
    {
        // Columnas esperadas (orden flexible — se detecta por encabezado):
        // EmployeeId | ObjectId | DisplayName | UserPrincipalName | Email |
        // Department | JobTitle | AccountEnabled | Manager | OfficeLocation |
        // CreatedDateTime | LastSignInDateTime
        var (filas, errores) = ParseExcel(request.Contenido);

        var now = DateTime.UtcNow;
        var nombre = string.IsNullOrWhiteSpace(request.NombreInstantanea)
            ? $"Entra ID — {now:dd/MM/yyyy HH:mm}"
            : request.NombreInstantanea.Trim();

        var snapshot = new SnapshotEntraID
        {
            Id               = Guid.NewGuid(),
            Nombre           = nombre,
            FechaInstantanea = now,
            TotalRegistros   = filas.Count,
            CreadoPor        = _user.Email
        };
        await _snapshotRepo.AddAsync(snapshot, ct);

        foreach (var fila in filas)
        {
            fila.SnapshotId = snapshot.Id;
            await _registroRepo.AddAsync(fila, ct);
        }

        try
        {
            await _registroRepo.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            errores.Insert(0, $"Error al guardar: {ex.InnerException?.Message ?? ex.Message}");
            return new CargarSnapshotEntraIDResultado(
                snapshot.Id, nombre, now, filas.Count, errores.Count, errores);
        }

        await _audit.LogAsync(_user.UserId, _user.Email, "CARGA_SNAPSHOT_ENTRAID", "SnapshotEntraID",
            snapshot.Id.ToString(), datosDespues: new { snapshot.TotalRegistros }, ct: ct);

        return new CargarSnapshotEntraIDResultado(
            snapshot.Id, nombre, now, filas.Count, errores.Count, errores);
    }

    private static (List<RegistroEntraID> filas, List<string> errores) ParseExcel(Stream stream)
    {
        var filas  = new List<RegistroEntraID>();
        var errores = new List<string>();

        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow < 2) return (filas, errores);

        // Detectar columnas por nombre de encabezado (fila 1)
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;
        for (int c = 1; c <= lastCol; c++)
        {
            var h = ws.Cell(1, c).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(h) && !headers.ContainsKey(h))
                headers[h] = c;
        }

        int Col(params string[] names)
        {
            foreach (var n in names)
                if (headers.TryGetValue(n, out var col)) return col;
            return 0;
        }

        int cEmployeeId        = Col("EmployeeId","employeeid","Cedula","ID");
        int cObjectId          = Col("ObjectId","Id","objectId","id");
        int cDisplayName       = Col("DisplayName","displayName","NombreCompleto","Nombre");
        int cUpn               = Col("UserPrincipalName","userPrincipalName","UPN","Email","Mail");
        int cEmail             = Col("Email","mail","email","UserPrincipalName","userPrincipalName");
        int cDepartment        = Col("Department","department","Departamento");
        int cJobTitle          = Col("JobTitle","jobTitle","Puesto","Title");
        int cEnabled           = Col("AccountEnabled","accountEnabled","Enabled","Activo","Estado");
        int cManager           = Col("Manager","manager","Jefe","ManagerDisplayName");
        int cOffice            = Col("OfficeLocation","officeLocation","Oficina");
        int cCreated           = Col("CreatedDateTime","createdDateTime","FechaCreacion");
        int cLastSign          = Col("LastSignInDateTime","lastSignInDateTime","UltimoIngreso","SignIn");

        string Get(int r, int c) => c > 0 ? ws.Cell(r, c).GetString().Trim() : string.Empty;
        bool GetBool(int r, int c)
        {
            if (c == 0) return true;
            var v = ws.Cell(r, c).GetString().Trim().ToUpperInvariant();
            return v is "TRUE" or "1" or "SI" or "SÍ" or "YES" or "ACTIVO" or "";
        }
        DateTime? GetDate(int r, int c)
        {
            if (c == 0) return null;
            return ws.Cell(r, c).TryGetValue<DateTime>(out var d) ? d : null;
        }

        for (int r = 2; r <= lastRow; r++)
        {
            var displayName = Get(r, cDisplayName);
            var upn         = Get(r, cUpn);
            if (string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(upn))
                continue; // fila vacía

            try
            {
                filas.Add(new RegistroEntraID
                {
                    EmployeeId        = NullIfEmpty(Get(r, cEmployeeId)),
                    ObjectId          = NullIfEmpty(Get(r, cObjectId)),
                    DisplayName       = NullIfEmpty(displayName),
                    UserPrincipalName = NullIfEmpty(upn),
                    Email             = NullIfEmpty(Get(r, cEmail)),
                    Department        = NullIfEmpty(Get(r, cDepartment)),
                    JobTitle          = NullIfEmpty(Get(r, cJobTitle)),
                    AccountEnabled    = GetBool(r, cEnabled),
                    Manager           = NullIfEmpty(Get(r, cManager)),
                    OfficeLocation    = NullIfEmpty(Get(r, cOffice)),
                    CreatedDateTime   = GetDate(r, cCreated),
                    LastSignInDateTime = GetDate(r, cLastSign),
                });
            }
            catch (Exception ex)
            {
                errores.Add($"Fila {r}: {ex.Message}");
            }
        }

        return (filas, errores);
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}

// ─── Listar Snapshots ────────────────────────────────────────────────────────

public record GetSnapshotsEntraIDQuery : IRequest<List<SnapshotEntraIDDto>>;

public record SnapshotEntraIDDto(
    Guid Id,
    string Nombre,
    DateTime FechaInstantanea,
    int TotalRegistros,
    string? CreadoPor
);

public class GetSnapshotsEntraIDHandler : IRequestHandler<GetSnapshotsEntraIDQuery, List<SnapshotEntraIDDto>>
{
    private readonly IRepository<SnapshotEntraID> _repo;
    public GetSnapshotsEntraIDHandler(IRepository<SnapshotEntraID> repo) => _repo = repo;

    public async Task<List<SnapshotEntraIDDto>> Handle(
        GetSnapshotsEntraIDQuery request, CancellationToken ct)
    {
        var all = await _repo.GetAllAsync(ct);
        return all
            .OrderByDescending(s => s.FechaInstantanea)
            .Select(s => new SnapshotEntraIDDto(
                s.Id, s.Nombre, s.FechaInstantanea, s.TotalRegistros, s.CreadoPor))
            .ToList();
    }
}

// ─── Descargar Snapshot como Excel ───────────────────────────────────────────

public record DescargarSnapshotEntraIDQuery(Guid SnapshotId) : IRequest<DescargarSnapshotResult>;

public record DescargarSnapshotResult(
    byte[] Contenido,
    string NombreArchivo
);

public class DescargarSnapshotEntraIDHandler
    : IRequestHandler<DescargarSnapshotEntraIDQuery, DescargarSnapshotResult>
{
    private readonly IRepository<SnapshotEntraID> _snapshotRepo;
    private readonly IRepository<RegistroEntraID> _registroRepo;

    public DescargarSnapshotEntraIDHandler(
        IRepository<SnapshotEntraID> snapshotRepo,
        IRepository<RegistroEntraID> registroRepo)
    { _snapshotRepo = snapshotRepo; _registroRepo = registroRepo; }

    public async Task<DescargarSnapshotResult> Handle(
        DescargarSnapshotEntraIDQuery request, CancellationToken ct)
    {
        var snap = await _snapshotRepo.GetByIdAsync(request.SnapshotId, ct)
            ?? throw new KeyNotFoundException($"Snapshot {request.SnapshotId} no encontrado.");

        var registros = (await _registroRepo.FindAsync(
            r => r.SnapshotId == request.SnapshotId, ct)).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("EntraID");

        // Encabezados
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
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Metadata en fila 2 (color)
        ws.Cell(2, 1).Value = $"Instantánea: {snap.Nombre} | Fecha: {snap.FechaInstantanea:dd/MM/yyyy HH:mm} | Total: {snap.TotalRegistros}";
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#374151");
        ws.Range(2, 1, 2, headers.Length).Merge();

        // Datos desde fila 3
        int row = 3;
        foreach (var r in registros)
        {
            ws.Cell(row, 1).Value  = r.EmployeeId ?? "";
            ws.Cell(row, 2).Value  = r.ObjectId ?? "";
            ws.Cell(row, 3).Value  = r.DisplayName ?? "";
            ws.Cell(row, 4).Value  = r.UserPrincipalName ?? "";
            ws.Cell(row, 5).Value  = r.Email ?? "";
            ws.Cell(row, 6).Value  = r.Department ?? "";
            ws.Cell(row, 7).Value  = r.JobTitle ?? "";
            ws.Cell(row, 8).Value  = r.AccountEnabled ? "TRUE" : "FALSE";
            ws.Cell(row, 9).Value  = r.Manager ?? "";
            ws.Cell(row, 10).Value = r.OfficeLocation ?? "";
            ws.Cell(row, 11).Value = r.CreatedDateTime?.ToString("dd/MM/yyyy HH:mm") ?? "";
            ws.Cell(row, 12).Value = r.LastSignInDateTime?.ToString("dd/MM/yyyy HH:mm") ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var safeName = snap.Nombre.Replace("/", "-").Replace(":", "-").Replace(" ", "_");
        return new DescargarSnapshotResult(
            ms.ToArray(),
            $"EntraID_Snapshot_{safeName}_{snap.FechaInstantanea:yyyyMMdd_HHmm}.xlsx"
        );
    }
}
