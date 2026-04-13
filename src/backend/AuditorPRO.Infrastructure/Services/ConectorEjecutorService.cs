using AuditorPRO.Application.Features.Conectores;
using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MediatR;
using System.Diagnostics;
using System.Text.Json;

namespace AuditorPRO.Infrastructure.Services;

// ── Handler: Probar conector (sobreescribe el handler de Application) ─────────
public class ProbarConectorSqlHandler : IRequestHandler<ProbarConectorCommand, ProbarConectorResult>
{
    private readonly IRepository<Conector> _repo;
    private readonly IRepository<LogConector> _logRepo;
    private readonly ICurrentUserService _user;
    private readonly IConfiguration _config;
    private readonly ILogger<ProbarConectorSqlHandler> _logger;

    public ProbarConectorSqlHandler(
        IRepository<Conector> repo, IRepository<LogConector> logRepo,
        ICurrentUserService user, IConfiguration config,
        ILogger<ProbarConectorSqlHandler> logger)
    { _repo = repo; _logRepo = logRepo; _user = user; _config = config; _logger = logger; }

    public async Task<ProbarConectorResult> Handle(ProbarConectorCommand request, CancellationToken ct)
    {
        var conector = await _repo.GetByIdAsync(request.ConectorId, ct)
            ?? throw new KeyNotFoundException($"Conector {request.ConectorId} no encontrado.");

        var sw = Stopwatch.StartNew();
        bool exitoso;
        string mensaje;

        try
        {
            if (conector.TipoConector == TipoConector.BASE_DATOS)
            {
                var connStr = BuildSqlConnectionString(conector);
                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync(ct);
                exitoso = true;
                mensaje = $"Conexión exitosa a {conn.DataSource} / {conn.Database}";
                await conn.CloseAsync();
            }
            else if (!string.IsNullOrEmpty(conector.UrlEndpoint))
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await http.GetAsync(conector.UrlEndpoint, ct);
                exitoso = (int)response.StatusCode < 500;
                mensaje = exitoso ? $"HTTP {(int)response.StatusCode} — OK" : $"HTTP {(int)response.StatusCode}";
            }
            else
            {
                exitoso = true;
                mensaje = "Conector configurado (sin prueba de conectividad directa)";
            }
        }
        catch (Exception ex)
        {
            exitoso = false;
            mensaje = ex.Message;
            _logger.LogWarning(ex, "Probar conector {Id} falló", conector.Id);
        }

        sw.Stop();
        conector.UltimaEjecucion = DateTime.UtcNow;
        conector.UltimaEjecucionExito = exitoso;
        conector.UltimoError = exitoso ? null : mensaje;
        conector.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(conector, ct);
        await _logRepo.AddAsync(new LogConector
        {
            ConectorId = conector.Id, Exitoso = exitoso,
            DuracionMs = (int)sw.ElapsedMilliseconds,
            MensajeError = exitoso ? null : mensaje,
            EjecutadoPor = _user.Email
        }, ct);
        await _repo.SaveChangesAsync(ct);

        return new ProbarConectorResult { Exitoso = exitoso, Mensaje = mensaje, DuracionMs = (int)sw.ElapsedMilliseconds };
    }

    private string BuildSqlConnectionString(Conector conector)
    {
        var cfg = ParseConfig(conector.ConfiguracionJson);
        var servidor  = cfg.GetValueOrDefault("servidor", "");
        var baseDatos = cfg.GetValueOrDefault("baseDatos", "");
        var usuario   = cfg.GetValueOrDefault("usuario", "");

        // La contraseña viene de Key Vault (cargado en IConfiguration) o del campo passwordPlain (solo dev)
        var password = string.Empty;
        if (!string.IsNullOrEmpty(conector.SecretKeyVaultRef))
            password = _config[conector.SecretKeyVaultRef] ?? string.Empty;
        if (string.IsNullOrEmpty(password))
            password = cfg.GetValueOrDefault("passwordPlain", "");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource         = servidor,
            InitialCatalog     = baseDatos,
            UserID             = usuario,
            Password           = password,
            Encrypt            = true,
            TrustServerCertificate = true,
            ConnectTimeout     = 15,
        };
        return builder.ConnectionString;
    }

    internal static Dictionary<string, string> ParseConfig(string json)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? []; }
        catch { return []; }
    }
}

// ── Handler: Probar query personalizado (sin persistir nada) ─────────────────
public class ProbarQueryHandler : IRequestHandler<ProbarQueryCommand, EjecutarConectorResult>
{
    private readonly IRepository<Conector> _repo;
    private readonly IConfiguration _config;
    private readonly ILogger<ProbarQueryHandler> _logger;

    public ProbarQueryHandler(IRepository<Conector> repo, IConfiguration config, ILogger<ProbarQueryHandler> logger)
    { _repo = repo; _config = config; _logger = logger; }

    public async Task<EjecutarConectorResult> Handle(ProbarQueryCommand request, CancellationToken ct)
    {
        var conector = await _repo.GetByIdAsync(request.ConectorId, ct)
            ?? throw new KeyNotFoundException($"Conector {request.ConectorId} no encontrado.");

        // Usar la config enviada por el frontend (no guardada) si se proporcionó
        var cfgJson = !string.IsNullOrWhiteSpace(request.ConfiguracionJsonOverride)
            ? request.ConfiguracionJsonOverride
            : conector.ConfiguracionJson;

        var sw = Stopwatch.StartNew();
        var result = new EjecutarConectorResult();

        try
        {
            var cfg   = ProbarConectorSqlHandler.ParseConfig(cfgJson);
            var vista = cfg.GetValueOrDefault("vista", "");
            var query = cfg.GetValueOrDefault("queryPersonalizado", "");
            if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(vista))
                throw new InvalidOperationException("Debes definir una vista/tabla o un query personalizado.");

            var sql = !string.IsNullOrWhiteSpace(query) ? query : $"SELECT TOP 50 * FROM {vista}";

            // Contraseña: Key Vault primero, luego passwordPlain de la config enviada
            var password = string.Empty;
            if (!string.IsNullOrEmpty(conector.SecretKeyVaultRef))
                password = _config[conector.SecretKeyVaultRef] ?? string.Empty;
            if (string.IsNullOrEmpty(password))
                password = cfg.GetValueOrDefault("passwordPlain", "");

            var connStr = new SqlConnectionStringBuilder
            {
                DataSource            = cfg.GetValueOrDefault("servidor", ""),
                InitialCatalog        = cfg.GetValueOrDefault("baseDatos", ""),
                UserID                = cfg.GetValueOrDefault("usuario", ""),
                Password              = password,
                Encrypt               = true,
                TrustServerCertificate = true,
                ConnectTimeout        = 15,
            }.ConnectionString;

            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 20 };
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            for (int i = 0; i < reader.FieldCount; i++)
                result.Columnas.Add(reader.GetName(i));

            int count = 0;
            while (await reader.ReadAsync(ct) && count < 50)
            {
                var fila = new List<object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                    fila.Add(reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString());
                result.Filas.Add(fila);
                count++;
            }
            result.TotalFilas = count;
            result.Exitoso = true;
            result.Mensaje = $"{count} filas — query ejecutado correctamente";
        }
        catch (Exception ex)
        {
            result.Exitoso = false;
            result.Mensaje = ex.Message;
            _logger.LogWarning(ex, "ProbarQuery conector {Id} falló", conector.Id);
        }

        sw.Stop();
        result.DuracionMs = (int)sw.ElapsedMilliseconds;
        return result;
    }
}

// ── Handler: Ejecutar conector y devolver filas ───────────────────────────────
public class EjecutarConectorHandler : IRequestHandler<EjecutarConectorCommand, EjecutarConectorResult>
{
    private readonly IRepository<Conector> _repo;
    private readonly IRepository<LogConector> _logRepo;
    private readonly ICurrentUserService _user;
    private readonly IConfiguration _config;
    private readonly ILogger<EjecutarConectorHandler> _logger;

    public EjecutarConectorHandler(
        IRepository<Conector> repo, IRepository<LogConector> logRepo,
        ICurrentUserService user, IConfiguration config,
        ILogger<EjecutarConectorHandler> logger)
    { _repo = repo; _logRepo = logRepo; _user = user; _config = config; _logger = logger; }

    public async Task<EjecutarConectorResult> Handle(EjecutarConectorCommand request, CancellationToken ct)
    {
        var conector = await _repo.GetByIdAsync(request.ConectorId, ct)
            ?? throw new KeyNotFoundException($"Conector {request.ConectorId} no encontrado.");

        var sw = Stopwatch.StartNew();
        var result = new EjecutarConectorResult();

        try
        {
            if (conector.TipoConector == TipoConector.BASE_DATOS)
                await EjecutarSql(conector, request.MaxFilas, result, ct);
            else
                throw new InvalidOperationException($"Ejecución directa no soportada para tipo {conector.TipoConector}. Use el botón Probar para conectores HTTP/REST.");

            result.Exitoso = true;
        }
        catch (Exception ex)
        {
            result.Exitoso = false;
            result.Mensaje = ex.Message;
            _logger.LogWarning(ex, "Ejecutar conector {Id} falló", conector.Id);
        }

        sw.Stop();
        result.DuracionMs = (int)sw.ElapsedMilliseconds;

        conector.UltimaEjecucion = DateTime.UtcNow;
        conector.UltimaEjecucionExito = result.Exitoso;
        conector.UltimoError = result.Exitoso ? null : result.Mensaje;
        conector.TotalEjecuciones++;
        conector.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(conector, ct);
        await _logRepo.AddAsync(new LogConector
        {
            ConectorId = conector.Id, Exitoso = result.Exitoso,
            DuracionMs = result.DuracionMs, RegistrosProcesados = result.TotalFilas,
            MensajeError = result.Exitoso ? null : result.Mensaje,
            EjecutadoPor = _user.Email
        }, ct);
        await _repo.SaveChangesAsync(ct);

        return result;
    }

    private async Task EjecutarSql(Conector conector, int maxFilas, EjecutarConectorResult result, CancellationToken ct)
    {
        var cfg    = ProbarConectorSqlHandler.ParseConfig(conector.ConfiguracionJson);
        var vista  = cfg.GetValueOrDefault("vista", "");
        var query  = cfg.GetValueOrDefault("queryPersonalizado", "");
        var sql    = !string.IsNullOrWhiteSpace(query) ? query : $"SELECT TOP {maxFilas} * FROM {vista}";

        var connStr = BuildSqlConnectionString(conector);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        // Columnas
        for (int i = 0; i < reader.FieldCount; i++)
            result.Columnas.Add(reader.GetName(i));

        // Filas
        int count = 0;
        while (await reader.ReadAsync(ct) && count < maxFilas)
        {
            var fila = new List<object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                fila.Add(reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString());
            result.Filas.Add(fila);
            count++;
        }

        result.TotalFilas = count;
        result.Mensaje = $"{count} registros obtenidos de {(string.IsNullOrEmpty(vista) ? "query personalizado" : vista)}";
    }

    private string BuildSqlConnectionString(Conector conector)
    {
        var cfg = ProbarConectorSqlHandler.ParseConfig(conector.ConfiguracionJson);
        var password = string.Empty;
        if (!string.IsNullOrEmpty(conector.SecretKeyVaultRef))
            password = _config[conector.SecretKeyVaultRef] ?? string.Empty;
        if (string.IsNullOrEmpty(password))
            password = cfg.GetValueOrDefault("passwordPlain", "");

        return new SqlConnectionStringBuilder
        {
            DataSource         = cfg.GetValueOrDefault("servidor", ""),
            InitialCatalog     = cfg.GetValueOrDefault("baseDatos", ""),
            UserID             = cfg.GetValueOrDefault("usuario", ""),
            Password           = password,
            Encrypt            = true,
            TrustServerCertificate = true,
            ConnectTimeout     = 15,
        }.ConnectionString;
    }
}
