using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuditorPRO.Application.Features.Simulaciones;

public record EjecutarSimulacionCommand(Guid SimulacionId) : IRequest<EjecutarSimulacionResult>;

public class EjecutarSimulacionResult
{
    public Guid SimulacionId { get; set; }
    public decimal ScoreMadurez { get; set; }
    public decimal PorcentajeCumplimiento { get; set; }
    public int TotalControles { get; set; }
    public int ControlesVerde { get; set; }
    public int ControlesAmarillo { get; set; }
    public int ControlesRojo { get; set; }
    public int HallazgosGenerados { get; set; }
    public int DuracionSegundos { get; set; }
}

public class EjecutarSimulacionHandler : IRequestHandler<EjecutarSimulacionCommand, EjecutarSimulacionResult>
{
    private readonly ISimulacionRepository _repo;
    private readonly IMotorReglasService _motor;
    private readonly IRepository<ResultadoControl> _resultadoRepo;
    private readonly IRepository<Hallazgo> _hallazgoRepo;
    private readonly IAuditLoggerService _auditLogger;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<EjecutarSimulacionHandler> _logger;

    public EjecutarSimulacionHandler(
        ISimulacionRepository repo,
        IMotorReglasService motor,
        IRepository<ResultadoControl> resultadoRepo,
        IRepository<Hallazgo> hallazgoRepo,
        IAuditLoggerService auditLogger,
        ICurrentUserService currentUser,
        ILogger<EjecutarSimulacionHandler> logger)
    {
        _repo = repo;
        _motor = motor;
        _resultadoRepo = resultadoRepo;
        _hallazgoRepo = hallazgoRepo;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<EjecutarSimulacionResult> Handle(EjecutarSimulacionCommand request, CancellationToken ct)
    {
        var simulacion = await _repo.GetByIdAsync(request.SimulacionId, ct)
            ?? throw new KeyNotFoundException($"Simulación {request.SimulacionId} no encontrada.");

        if (simulacion.Estado == EstadoSimulacion.COMPLETADA)
            throw new InvalidOperationException("La simulación ya fue completada.");

        simulacion.Estado = EstadoSimulacion.EN_PROCESO;
        simulacion.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(simulacion, ct);
        await _repo.SaveChangesAsync(ct);

        var inicio = DateTime.UtcNow;

        try
        {
            var resultado = await _motor.EjecutarSimulacionAsync(simulacion, ct);

            foreach (var r in resultado.Resultados)
                await _resultadoRepo.AddAsync(r, ct);

            foreach (var h in resultado.HallazgosGenerados)
                await _hallazgoRepo.AddAsync(h, ct);

            simulacion.Estado = EstadoSimulacion.COMPLETADA;
            simulacion.CompletadaAt = DateTime.UtcNow;
            simulacion.DuracionSegundos = (int)(DateTime.UtcNow - inicio).TotalSeconds;
            simulacion.TotalControles = resultado.TotalControles;
            simulacion.ControlesVerde = resultado.ControlesVerde;
            simulacion.ControlesAmarillo = resultado.ControlesAmarillo;
            simulacion.ControlesRojo = resultado.ControlesRojo;
            simulacion.ScoreMadurez = resultado.ScoreMadurez;
            simulacion.PorcentajeCumplimiento = resultado.PorcentajeCumplimiento;
            simulacion.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(simulacion, ct);
            await _repo.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(_currentUser.UserId, _currentUser.Email,
                "EJECUTAR_SIMULACION", "SimulacionAuditoria", simulacion.Id.ToString(),
                datosDespues: new { resultado.ScoreMadurez, resultado.TotalControles }, ct: ct);

            return new EjecutarSimulacionResult
            {
                SimulacionId = simulacion.Id,
                ScoreMadurez = resultado.ScoreMadurez,
                PorcentajeCumplimiento = resultado.PorcentajeCumplimiento,
                TotalControles = resultado.TotalControles,
                ControlesVerde = resultado.ControlesVerde,
                ControlesAmarillo = resultado.ControlesAmarillo,
                ControlesRojo = resultado.ControlesRojo,
                HallazgosGenerados = resultado.HallazgosGenerados.Count,
                DuracionSegundos = simulacion.DuracionSegundos ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando simulación {Id}", simulacion.Id);
            simulacion.Estado = EstadoSimulacion.ERROR;
            simulacion.ErrorDetalle = ex.Message;
            simulacion.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(simulacion, ct);
            await _repo.SaveChangesAsync(ct);
            throw;
        }
    }
}
