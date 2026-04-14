using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.Sociedades;

public record GetSociedadesQuery(bool? SoloActivas = null) : IRequest<IEnumerable<SociedadDto>>;

public record GetSociedadByCodigoQuery(string Codigo) : IRequest<SociedadDto?>;

public class SociedadDto
{
    public int    Id     { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Pais  { get; set; }
    public bool   Activa { get; set; }
}

public class GetSociedadesHandler : IRequestHandler<GetSociedadesQuery, IEnumerable<SociedadDto>>
{
    private readonly IRepository<Sociedad> _repo;

    public GetSociedadesHandler(IRepository<Sociedad> repo) => _repo = repo;

    public async Task<IEnumerable<SociedadDto>> Handle(GetSociedadesQuery request, CancellationToken cancellationToken)
    {
        var all = await _repo.GetAllAsync(cancellationToken);

        if (request.SoloActivas == true)
            all = all.Where(s => s.Activa);

        return all
            .OrderBy(s => s.Pais)
            .ThenBy(s => s.Codigo)
            .Select(s => new SociedadDto
            {
                Id     = s.Id,
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                Pais   = s.Pais,
                Activa = s.Activa,
            });
    }
}

public class GetSociedadByCodigoHandler : IRequestHandler<GetSociedadByCodigoQuery, SociedadDto?>
{
    private readonly IRepository<Sociedad> _repo;

    public GetSociedadByCodigoHandler(IRepository<Sociedad> repo) => _repo = repo;

    public async Task<SociedadDto?> Handle(GetSociedadByCodigoQuery request, CancellationToken cancellationToken)
    {
        var results = await _repo.FindAsync(s => s.Codigo == request.Codigo, cancellationToken);
        var s = results.FirstOrDefault();
        if (s is null) return null;

        return new SociedadDto
        {
            Id     = s.Id,
            Codigo = s.Codigo,
            Nombre = s.Nombre,
            Pais   = s.Pais,
            Activa = s.Activa,
        };
    }
}
