using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AuditorPRO.Application.Features.PlanesAccion;

public record CrearPlanAccionCommand(
    Guid HallazgoId,
    string Descripcion,
    string Responsable,
    DateOnly FechaCompromiso,
    string? Recursos
) : IRequest<Guid>;

public class CrearPlanAccionValidator : AbstractValidator<CrearPlanAccionCommand>
{
    public CrearPlanAccionValidator()
    {
        RuleFor(x => x.Descripcion).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Responsable).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FechaCompromiso).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha de compromiso debe ser futura.");
    }
}

public class CrearPlanAccionHandler : IRequestHandler<CrearPlanAccionCommand, Guid>
{
    private readonly IHallazgoRepository _hallazgoRepo;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;

    public CrearPlanAccionHandler(IHallazgoRepository hallazgoRepo, ICurrentUserService user, IAuditLoggerService audit)
    { _hallazgoRepo = hallazgoRepo; _user = user; _audit = audit; }

    public async Task<Guid> Handle(CrearPlanAccionCommand request, CancellationToken ct)
    {
        var hallazgo = await _hallazgoRepo.GetByIdAsync(request.HallazgoId, ct)
            ?? throw new KeyNotFoundException($"Hallazgo {request.HallazgoId} no encontrado.");

        hallazgo.PlanAccion = request.Descripcion;
        hallazgo.ResponsableEmail = request.Responsable;
        hallazgo.FechaCompromiso = request.FechaCompromiso;
        hallazgo.Estado = EstadoHallazgo.EN_PROCESO;
        hallazgo.UpdatedAt = DateTime.UtcNow;

        await _hallazgoRepo.UpdateAsync(hallazgo, ct);
        await _hallazgoRepo.SaveChangesAsync(ct);

        await _audit.LogAsync(_user.UserId, _user.Email, "CREAR_PLAN_ACCION", "Hallazgo",
            hallazgo.Id.ToString(), ct: ct);

        return hallazgo.Id;
    }
}

public record ActualizarEstatusAccionCommand(
    Guid HallazgoId,
    EstadoHallazgo NuevoEstado,
    string? Comentario,
    int? PorcentajeAvance
) : IRequest;

public class ActualizarEstatusAccionHandler : IRequestHandler<ActualizarEstatusAccionCommand>
{
    private readonly IHallazgoRepository _hallazgoRepo;
    private readonly ICurrentUserService _user;

    public ActualizarEstatusAccionHandler(IHallazgoRepository hallazgoRepo, ICurrentUserService user)
    { _hallazgoRepo = hallazgoRepo; _user = user; }

    public async Task Handle(ActualizarEstatusAccionCommand request, CancellationToken ct)
    {
        var hallazgo = await _hallazgoRepo.GetByIdAsync(request.HallazgoId, ct)
            ?? throw new KeyNotFoundException($"Hallazgo {request.HallazgoId} no encontrado.");

        hallazgo.Estado = request.NuevoEstado;
        if (request.NuevoEstado == EstadoHallazgo.CERRADO)
            hallazgo.FechaCierre = DateOnly.FromDateTime(DateTime.UtcNow);

        hallazgo.UpdatedAt = DateTime.UtcNow;

        await _hallazgoRepo.UpdateAsync(hallazgo, ct);
        await _hallazgoRepo.SaveChangesAsync(ct);
    }
}
