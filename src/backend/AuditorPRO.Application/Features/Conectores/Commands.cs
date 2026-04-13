using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AuditorPRO.Application.Features.Conectores;

public record CrearConectorCommand(
    string Nombre, string Sistema, string? Descripcion,
    TipoConector TipoConexion, string? UrlEndpoint, string? AuthType,
    string? SecretKeyVaultRef, string? ConfiguracionJson
) : IRequest<Guid>;

public class CrearConectorValidator : AbstractValidator<CrearConectorCommand>
{
    public CrearConectorValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sistema).NotEmpty().MaximumLength(50);
    }
}

public class CrearConectorHandler : IRequestHandler<CrearConectorCommand, Guid>
{
    private readonly IRepository<Conector> _repo;
    private readonly ICurrentUserService _user;

    public CrearConectorHandler(IRepository<Conector> repo, ICurrentUserService user)
    { _repo = repo; _user = user; }

    public async Task<Guid> Handle(CrearConectorCommand request, CancellationToken ct)
    {
        var c = new Conector
        {
            Nombre = request.Nombre, Sistema = request.Sistema, Descripcion = request.Descripcion,
            TipoConector = request.TipoConexion, UrlEndpoint = request.UrlEndpoint,
            AuthType = request.AuthType, SecretKeyVaultRef = request.SecretKeyVaultRef,
            ConfiguracionJson = request.ConfiguracionJson ?? "{}",
            Estado = EstadoConector.ACTIVO, CreatedBy = _user.Email
        };
        await _repo.AddAsync(c, ct);
        await _repo.SaveChangesAsync(ct);
        return c.Id;
    }
}

public record ActualizarConectorCommand(
    Guid Id, string Nombre, string Sistema, string? Descripcion,
    TipoConector TipoConexion, string? UrlEndpoint, string? AuthType, string? SecretKeyVaultRef,
    EstadoConector Estado, string? ConfiguracionJson
) : IRequest;

public class ActualizarConectorHandler : IRequestHandler<ActualizarConectorCommand>
{
    private readonly IRepository<Conector> _repo;
    public ActualizarConectorHandler(IRepository<Conector> repo) => _repo = repo;

    public async Task Handle(ActualizarConectorCommand request, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException($"Conector {request.Id} no encontrado.");
        c.Nombre = request.Nombre; c.Sistema = request.Sistema; c.Descripcion = request.Descripcion;
        c.TipoConector = request.TipoConexion; c.UrlEndpoint = request.UrlEndpoint;
        c.AuthType = request.AuthType; c.SecretKeyVaultRef = request.SecretKeyVaultRef;
        c.Estado = request.Estado;
        if (request.ConfiguracionJson != null) c.ConfiguracionJson = request.ConfiguracionJson;
        c.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(c, ct);
        await _repo.SaveChangesAsync(ct);
    }
}

public record ProbarConectorCommand(Guid ConectorId) : IRequest<ProbarConectorResult>;

public class ProbarConectorResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int DuracionMs { get; set; }
}

// ── Eliminar conector ─────────────────────────────────────────────────────────
public record EliminarConectorCommand(Guid Id) : IRequest;

public class EliminarConectorHandler : IRequestHandler<EliminarConectorCommand>
{
    private readonly IRepository<Conector> _repo;
    public EliminarConectorHandler(IRepository<Conector> repo) => _repo = repo;

    public async Task Handle(EliminarConectorCommand request, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException($"Conector {request.Id} no encontrado.");
        c.IsDeleted = true;
        c.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(c, ct);
        await _repo.SaveChangesAsync(ct);
    }
}

// ── Ejecutar conector (SQL/REST) y devolver resultados ────────────────────────
public record EjecutarConectorCommand(Guid ConectorId, int MaxFilas = 500) : IRequest<EjecutarConectorResult>;

public class EjecutarConectorResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int DuracionMs { get; set; }
    public int TotalFilas { get; set; }
    public List<string> Columnas { get; set; } = [];
    public List<List<object?>> Filas { get; set; } = [];
}

// ── Probar query personalizado (sin guardar) ──────────────────────────────────
public record ProbarQueryCommand(Guid ConectorId, string? ConfiguracionJsonOverride = null) : IRequest<EjecutarConectorResult>;

public class ProbarConectorHandler : IRequestHandler<ProbarConectorCommand, ProbarConectorResult>
{
    private readonly IRepository<Conector> _repo;
    private readonly IRepository<LogConector> _logRepo;
    private readonly ICurrentUserService _user;

    public ProbarConectorHandler(IRepository<Conector> repo, IRepository<LogConector> logRepo, ICurrentUserService user)
    { _repo = repo; _logRepo = logRepo; _user = user; }

    public async Task<ProbarConectorResult> Handle(ProbarConectorCommand request, CancellationToken ct)
    {
        var conector = await _repo.GetByIdAsync(request.ConectorId, ct)
            ?? throw new KeyNotFoundException($"Conector {request.ConectorId} no encontrado.");

        var inicio = DateTime.UtcNow;
        bool exitoso;
        string mensaje;

        try
        {
            // Test básico de conectividad HTTP
            if (!string.IsNullOrEmpty(conector.UrlEndpoint))
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await http.GetAsync(conector.UrlEndpoint, ct);
                exitoso = response.IsSuccessStatusCode || (int)response.StatusCode < 500;
                mensaje = exitoso ? $"Conexión exitosa — HTTP {(int)response.StatusCode}" : $"HTTP {(int)response.StatusCode}";
            }
            else
            {
                exitoso = true;
                mensaje = "Conector configurado (sin URL de prueba directa)";
            }
        }
        catch (Exception ex)
        {
            exitoso = false;
            mensaje = $"Error de conexión: {ex.Message}";
        }

        var duracion = (int)(DateTime.UtcNow - inicio).TotalMilliseconds;

        conector.UltimaEjecucion = DateTime.UtcNow;
        conector.UltimaEjecucionExito = exitoso;
        conector.UltimoError = exitoso ? null : mensaje;
        conector.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(conector, ct);

        var log = new LogConector
        {
            ConectorId = conector.Id, Exitoso = exitoso, DuracionMs = duracion,
            MensajeError = exitoso ? null : mensaje, EjecutadoPor = _user.Email
        };
        await _logRepo.AddAsync(log, ct);
        await _repo.SaveChangesAsync(ct);

        return new ProbarConectorResult { Exitoso = exitoso, Mensaje = mensaje, DuracionMs = duracion };
    }
}
