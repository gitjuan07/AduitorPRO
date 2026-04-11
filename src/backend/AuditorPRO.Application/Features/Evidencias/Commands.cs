using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AuditorPRO.Application.Features.Evidencias;

public record SubirEvidenciaCommand(
    Stream Contenido,
    string NombreArchivo,
    string ContentType,
    long TamanoBytes,
    TipoEvidencia TipoEvidencia,
    string? Descripcion,
    Guid? HallazgoId,
    Guid? SimulacionId,
    int? SociedadId,
    string? PeriodoReferencia
) : IRequest<Guid>;

public class SubirEvidenciaValidator : AbstractValidator<SubirEvidenciaCommand>
{
    private static readonly string[] AllowedTypes =
        ["application/pdf", "application/msword",
         "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
         "application/vnd.ms-excel",
         "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
         "text/csv", "image/png", "image/jpeg", "image/jpg"];

    public SubirEvidenciaValidator()
    {
        RuleFor(x => x.NombreArchivo).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TamanoBytes).GreaterThan(0).LessThan(52_428_800); // 50 MB
        RuleFor(x => x.ContentType).Must(t => AllowedTypes.Contains(t))
            .WithMessage("Tipo de archivo no permitido.");
    }
}

public class SubirEvidenciaHandler : IRequestHandler<SubirEvidenciaCommand, Guid>
{
    private readonly IBlobStorageService _blob;
    private readonly IRepository<Evidencia> _repo;
    private readonly ICurrentUserService _user;
    private readonly IAuditLoggerService _audit;

    public SubirEvidenciaHandler(IBlobStorageService blob, IRepository<Evidencia> repo,
        ICurrentUserService user, IAuditLoggerService audit)
    { _blob = blob; _repo = repo; _user = user; _audit = audit; }

    public async Task<Guid> Handle(SubirEvidenciaCommand request, CancellationToken ct)
    {
        var blobUrl = await _blob.UploadAsync(
            request.Contenido, request.NombreArchivo, request.ContentType, "evidencias", ct);

        var evidencia = new Evidencia
        {
            NombreArchivo = request.NombreArchivo,
            DescripcionArchivo = request.Descripcion,
            ContentType = request.ContentType,
            TamanoBytes = request.TamanoBytes,
            BlobUrl = blobUrl,
            BlobContainer = "evidencias",
            TipoEvidencia = request.TipoEvidencia,
            HallazgoId = request.HallazgoId,
            SimulacionId = request.SimulacionId,
            SubidoPor = _user.Email,
            CreatedBy = _user.Email
        };

        await _repo.AddAsync(evidencia, ct);
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(_user.UserId, _user.Email, "CREAR", "Evidencia",
            evidencia.Id.ToString(), ct: ct);

        return evidencia.Id;
    }
}

public record EliminarEvidenciaCommand(Guid EvidenciaId) : IRequest;

public class EliminarEvidenciaHandler : IRequestHandler<EliminarEvidenciaCommand>
{
    private readonly IRepository<Evidencia> _repo;
    private readonly IBlobStorageService _blob;
    private readonly ICurrentUserService _user;

    public EliminarEvidenciaHandler(IRepository<Evidencia> repo, IBlobStorageService blob, ICurrentUserService user)
    { _repo = repo; _blob = blob; _user = user; }

    public async Task Handle(EliminarEvidenciaCommand request, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(request.EvidenciaId, ct)
            ?? throw new KeyNotFoundException($"Evidencia {request.EvidenciaId} no encontrada.");

        await _blob.DeleteAsync(e.BlobUrl, ct);
        e.IsDeleted = true;
        e.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(e, ct);
        await _repo.SaveChangesAsync(ct);
    }
}
