using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Interfaces;
using AuditorPRO.Infrastructure.Helpers;
using AuditorPRO.Infrastructure.Persistence;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace AuditorPRO.Infrastructure.Services;

public class EntraIdSyncService : IEntraIdSyncService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ICurrentUserService _user;
    private readonly ILogger<EntraIdSyncService> _logger;

    public EntraIdSyncService(
        AppDbContext db,
        IConfiguration config,
        ICurrentUserService user,
        ILogger<EntraIdSyncService> logger)
    {
        _db = db;
        _config = config;
        _user = user;
        _logger = logger;
    }

    public async Task<EntraIdSyncResultado> SincronizarAsync(
        string? nombreInstantanea, CancellationToken ct = default)
    {
        var errores = new List<string>();
        var registros = new List<RegistroEntraID>();

        var now = DateTime.UtcNow;
        var nombre = string.IsNullOrWhiteSpace(nombreInstantanea)
            ? $"Entra ID (Graph) — {now:dd/MM/yyyy HH:mm}"
            : nombreInstantanea.Trim();

        _logger.LogInformation("Iniciando sincronización Entra ID via Microsoft Graph. Snapshot: {Nombre}", nombre);

        try
        {
            var graphClient = BuildGraphClient();

            // Paginación: recuperar todos los usuarios del tenant
            int pagina = 1;
            var pageIterator = await graphClient.Users.GetAsync(req =>
            {
                req.QueryParameters.Select = [
                    "id", "displayName", "userPrincipalName", "mail",
                    "department", "jobTitle", "accountEnabled",
                    "employeeId", "officeLocation", "createdDateTime",
                    "signInActivity"
                ];
                req.QueryParameters.Top = 999;
            }, ct);

            if (pageIterator?.Value == null)
            {
                errores.Add("Graph no devolvió resultados. Verificar permisos User.Read.All.");
                return BuildResultado(Guid.Empty, nombre, now, 0, errores);
            }

            var usuarios = new List<User>();
            var pageResponse = pageIterator;

            while (pageResponse != null)
            {
                _logger.LogInformation("Procesando página {Pagina} de usuarios Graph ({Count} usuarios)", pagina, pageResponse.Value?.Count ?? 0);
                if (pageResponse.Value != null)
                    usuarios.AddRange(pageResponse.Value);

                if (pageResponse.OdataNextLink == null) break;

                pageResponse = await graphClient.Users.WithUrl(pageResponse.OdataNextLink)
                    .GetAsync(cancellationToken: ct);
                pagina++;
            }

            _logger.LogInformation("Total usuarios recuperados de Graph: {Total} en {Paginas} páginas", usuarios.Count, pagina);

            // Convertir a RegistroEntraID
            foreach (var u in usuarios)
            {
                try
                {
                    var empId = u.EmployeeId;
                    var registro = new RegistroEntraID
                    {
                        ObjectId          = u.Id,
                        DisplayName       = u.DisplayName,
                        UserPrincipalName = u.UserPrincipalName,
                        Email             = u.Mail ?? u.UserPrincipalName,
                        Department        = u.Department,
                        JobTitle          = u.JobTitle,
                        AccountEnabled    = u.AccountEnabled ?? false,
                        OfficeLocation    = u.OfficeLocation,
                        CreatedDateTime   = u.CreatedDateTime?.UtcDateTime,
                        EmployeeId        = empId,
                        EmployeeIdNormalizado = IdentityNormalizationHelper.NormalizarCedula(empId),
                        LastSignInDateTime = u.SignInActivity?.LastSignInDateTime?.UtcDateTime,
                    };

                    if (string.IsNullOrWhiteSpace(registro.DisplayName) && string.IsNullOrWhiteSpace(registro.UserPrincipalName))
                    {
                        errores.Add($"Usuario ID={u.Id} sin DisplayName ni UPN — omitido.");
                        continue;
                    }

                    registros.Add(registro);
                }
                catch (Exception ex)
                {
                    errores.Add($"Usuario ID={u.Id}: {ex.Message}");
                    _logger.LogWarning("Error procesando usuario {Id}: {Msg}", u.Id, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar con Microsoft Graph");
            var msg = ex is ServiceException se
                ? $"Error Graph ({se.ResponseStatusCode}): {se.Message}"
                : $"Error de conexión Graph: {ex.Message}";
            errores.Add(msg);
            return BuildResultado(Guid.Empty, nombre, now, 0, errores);
        }

        // Persistir snapshot y registros
        var snapshot = new SnapshotEntraID
        {
            Id               = Guid.NewGuid(),
            Nombre           = nombre,
            FechaInstantanea = now,
            TotalRegistros   = registros.Count,
            CreadoPor        = _user.Email,
            Origen           = "GRAPH_DIRECT",
        };

        _db.SnapshotsEntraID.Add(snapshot);

        foreach (var reg in registros)
        {
            reg.SnapshotId = snapshot.Id;
            _db.RegistrosEntraID.Add(reg);
        }

        try
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Snapshot Entra ID {Id} guardado: {Total} usuarios, {Errores} errores.",
                snapshot.Id, registros.Count, errores.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al persistir snapshot Entra ID");
            errores.Insert(0, $"Error al guardar snapshot: {ex.InnerException?.Message ?? ex.Message}");
            return BuildResultado(Guid.Empty, nombre, now, registros.Count, errores);
        }

        return BuildResultado(snapshot.Id, nombre, now, registros.Count, errores);
    }

    private GraphServiceClient BuildGraphClient()
    {
        // DefaultAzureCredential: usa Managed Identity en Azure, az login en dev
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeInteractiveBrowserCredential = true,
        });

        return new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
    }

    private static EntraIdSyncResultado BuildResultado(
        Guid id, string nombre, DateTime fecha, int total, List<string> errores) =>
        new(id, nombre, fecha, total, errores.Count, errores, "GRAPH_DIRECT");
}
