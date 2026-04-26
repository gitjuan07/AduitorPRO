using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Interfaces;
using MediatR;

namespace AuditorPRO.Application.Features.Cargas;

public record MatrizPuestoDto(
    string UsuarioSAP,
    string? NombreCompleto,
    string? Cedula,
    string? Sociedad,
    string? Departamento,
    string? Puesto,
    string? Email,
    string Rol,
    string? Transaccion,
    string? InicioValidez,
    string? FinValidez,
    string? UltimoIngreso,
    string? FechaRevisionContraloria
);

public record MatrizPuestosResultado(
    int Total,
    int Page,
    int PageSize,
    List<MatrizPuestoDto> Items
);

public record GetMatrizPuestosQuery(
    string? Usuario,
    string? Puesto,
    string? Rol,
    string? Transaccion,
    int Page,
    int PageSize
) : IRequest<MatrizPuestosResultado>;

public class GetMatrizPuestosHandler : IRequestHandler<GetMatrizPuestosQuery, MatrizPuestosResultado>
{
    private readonly IRepository<MatrizPuestoSAP> _repo;

    public GetMatrizPuestosHandler(IRepository<MatrizPuestoSAP> repo) => _repo = repo;

    public async Task<MatrizPuestosResultado> Handle(GetMatrizPuestosQuery request, CancellationToken ct)
    {
        var todos = await _repo.GetAllAsync(ct);

        var query = todos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Usuario))
            query = query.Where(m => m.UsuarioSAP.Contains(request.Usuario, StringComparison.OrdinalIgnoreCase)
                                  || (m.NombreCompleto != null && m.NombreCompleto.Contains(request.Usuario, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(request.Puesto))
            query = query.Where(m => m.Puesto.Contains(request.Puesto, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Rol))
            query = query.Where(m => m.Rol.Contains(request.Rol, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Transaccion))
            query = query.Where(m => m.Transaccion != null && m.Transaccion.Contains(request.Transaccion, StringComparison.OrdinalIgnoreCase));

        var total = query.Count();
        var items = query
            .OrderBy(m => m.UsuarioSAP).ThenBy(m => m.Rol).ThenBy(m => m.Transaccion)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList()
            .Select(m => new MatrizPuestoDto(
                m.UsuarioSAP,
                m.NombreCompleto,
                m.Cedula,
                m.Sociedad,
                m.Departamento,
                m.Puesto,
                m.Email,
                m.Rol,
                m.Transaccion,
                m.InicioValidez?.ToString("yyyy-MM-dd"),
                m.FinValidez?.ToString("yyyy-MM-dd"),
                m.UltimoIngreso?.ToString("yyyy-MM-dd"),
                m.FechaRevisionContraloria?.ToString("yyyy-MM-dd")
            ))
            .ToList();

        return new MatrizPuestosResultado(total, request.Page, request.PageSize, items);
    }
}

// ── Resultado genérico paginado ───────────────────────────────────────────────
public record PagedResultado<T>(int Total, int Page, int PageSize, List<T> Items);

// ── Query: Empleados ──────────────────────────────────────────────────────────
public record EmpleadoVisorDto(
    string NumeroEmpleado, string? Cedula, string NombreCompleto,
    string? CorreoCorporativo, string EstadoLaboral,
    string? FechaIngreso, string? FechaBaja);

public record GetEmpleadosVisorQuery(string? Q, string? Estado, int Page, int PageSize)
    : IRequest<PagedResultado<EmpleadoVisorDto>>;

public class GetEmpleadosVisorHandler : IRequestHandler<GetEmpleadosVisorQuery, PagedResultado<EmpleadoVisorDto>>
{
    private readonly IRepository<EmpleadoMaestro> _repo;
    public GetEmpleadosVisorHandler(IRepository<EmpleadoMaestro> repo) => _repo = repo;

    public async Task<PagedResultado<EmpleadoVisorDto>> Handle(GetEmpleadosVisorQuery req, CancellationToken ct)
    {
        var todos = await _repo.GetAllAsync(ct);
        var q = todos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Q))
            q = q.Where(e => e.NombreCompleto.Contains(req.Q, StringComparison.OrdinalIgnoreCase)
                          || (e.Cedula != null && e.Cedula.Contains(req.Q))
                          || e.NumeroEmpleado.Contains(req.Q, StringComparison.OrdinalIgnoreCase)
                          || (e.CorreoCorporativo != null && e.CorreoCorporativo.Contains(req.Q, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(req.Estado))
            q = q.Where(e => e.EstadoLaboral.ToString() == req.Estado);

        var total = q.Count();
        var items = q.OrderBy(e => e.NombreCompleto)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize).ToList()
            .Select(e => new EmpleadoVisorDto(
                e.NumeroEmpleado, e.Cedula, e.NombreCompleto,
                e.CorreoCorporativo, e.EstadoLaboral.ToString(),
                e.FechaIngreso?.ToString("dd/MM/yyyy"), e.FechaBaja?.ToString("dd/MM/yyyy")))
            .ToList();

        return new PagedResultado<EmpleadoVisorDto>(total, req.Page, req.PageSize, items);
    }
}

// ── Query: Usuarios SAP ───────────────────────────────────────────────────────
public record UsuarioSAPVisorDto(
    string NombreUsuario, string? Cedula, string? NombreCompleto,
    string? Sociedad, string? Departamento, string? Puesto,
    string? Email, string Estado, string? TipoUsuario, string? UltimoAcceso);

public record GetUsuariosSAPVisorQuery(string? Q, string? Estado, int Page, int PageSize)
    : IRequest<PagedResultado<UsuarioSAPVisorDto>>;

public class GetUsuariosSAPVisorHandler : IRequestHandler<GetUsuariosSAPVisorQuery, PagedResultado<UsuarioSAPVisorDto>>
{
    private readonly IRepository<UsuarioSistema> _repo;
    public GetUsuariosSAPVisorHandler(IRepository<UsuarioSistema> repo) => _repo = repo;

    public async Task<PagedResultado<UsuarioSAPVisorDto>> Handle(GetUsuariosSAPVisorQuery req, CancellationToken ct)
    {
        var todos = await _repo.GetAllAsync(ct);
        var q = todos.Where(u => u.Sistema == "SAP").AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Q))
            q = q.Where(u => u.NombreUsuario.Contains(req.Q, StringComparison.OrdinalIgnoreCase)
                          || (u.Cedula != null && u.Cedula.Contains(req.Q))
                          || (u.NombreCompleto != null && u.NombreCompleto.Contains(req.Q, StringComparison.OrdinalIgnoreCase))
                          || (u.Email != null && u.Email.Contains(req.Q, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(req.Estado))
            q = q.Where(u => u.Estado.ToString() == req.Estado);

        var total = q.Count();
        var items = q.OrderBy(u => u.NombreUsuario)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize).ToList()
            .Select(u => new UsuarioSAPVisorDto(
                u.NombreUsuario, u.Cedula, u.NombreCompleto,
                u.Sociedad, u.Departamento, u.Puesto,
                u.Email, u.Estado.ToString(), u.TipoUsuario,
                u.FechaUltimoAcceso.HasValue ? u.FechaUltimoAcceso.Value.ToString("dd/MM/yyyy") : null))
            .ToList();

        return new PagedResultado<UsuarioSAPVisorDto>(total, req.Page, req.PageSize, items);
    }
}

// ── Query: Casos SE Suite ─────────────────────────────────────────────────────
public record CasoSESuiteVisorDto(
    string NumeroCaso, string? Titulo, string? UsuarioSAP, string? Cedula,
    string? RolJustificado, string? FechaAprobacion, string? FechaVencimiento,
    string EstadoCaso, string? Aprobador);

public record GetCasosSESuiteVisorQuery(string? Q, string? Estado, int Page, int PageSize)
    : IRequest<PagedResultado<CasoSESuiteVisorDto>>;

public class GetCasosSESuiteVisorHandler : IRequestHandler<GetCasosSESuiteVisorQuery, PagedResultado<CasoSESuiteVisorDto>>
{
    private readonly IRepository<CasoSESuite> _repo;
    public GetCasosSESuiteVisorHandler(IRepository<CasoSESuite> repo) => _repo = repo;

    public async Task<PagedResultado<CasoSESuiteVisorDto>> Handle(GetCasosSESuiteVisorQuery req, CancellationToken ct)
    {
        var todos = await _repo.GetAllAsync(ct);
        var q = todos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Q))
            q = q.Where(c => c.NumeroCaso.Contains(req.Q, StringComparison.OrdinalIgnoreCase)
                          || (c.UsuarioSAP != null && c.UsuarioSAP.Contains(req.Q, StringComparison.OrdinalIgnoreCase))
                          || (c.Cedula != null && c.Cedula.Contains(req.Q))
                          || (c.RolJustificado != null && c.RolJustificado.Contains(req.Q, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(req.Estado))
            q = q.Where(c => c.EstadoCaso == req.Estado);

        var total = q.Count();
        var items = q.OrderByDescending(c => c.FechaVencimiento)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize).ToList()
            .Select(c => new CasoSESuiteVisorDto(
                c.NumeroCaso, c.Titulo, c.UsuarioSAP, c.Cedula,
                c.RolJustificado,
                c.FechaAprobacion?.ToString("dd/MM/yyyy"),
                c.FechaVencimiento?.ToString("dd/MM/yyyy"),
                c.EstadoCaso, c.Aprobador))
            .ToList();

        return new PagedResultado<CasoSESuiteVisorDto>(total, req.Page, req.PageSize, items);
    }
}

// ── Query: Registros Entra ID ─────────────────────────────────────────────────
public record RegistroEntraIDVisorDto(
    string? EmployeeId, string? DisplayName, string? UserPrincipalName,
    string? Email, string? Department, string? JobTitle,
    bool AccountEnabled, string? Manager, string? UltimoSignIn);

public record GetRegistrosEntraIDVisorQuery(Guid SnapshotId, string? Q, bool? AccountEnabled, int Page, int PageSize)
    : IRequest<PagedResultado<RegistroEntraIDVisorDto>>;

public class GetRegistrosEntraIDVisorHandler : IRequestHandler<GetRegistrosEntraIDVisorQuery, PagedResultado<RegistroEntraIDVisorDto>>
{
    private readonly IRepository<RegistroEntraID> _repo;
    public GetRegistrosEntraIDVisorHandler(IRepository<RegistroEntraID> repo) => _repo = repo;

    public async Task<PagedResultado<RegistroEntraIDVisorDto>> Handle(GetRegistrosEntraIDVisorQuery req, CancellationToken ct)
    {
        var todos = await _repo.GetAllAsync(ct);
        var q = todos.Where(r => r.SnapshotId == req.SnapshotId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Q))
            q = q.Where(r => (r.DisplayName != null && r.DisplayName.Contains(req.Q, StringComparison.OrdinalIgnoreCase))
                          || (r.EmployeeId != null && r.EmployeeId.Contains(req.Q))
                          || (r.UserPrincipalName != null && r.UserPrincipalName.Contains(req.Q, StringComparison.OrdinalIgnoreCase))
                          || (r.Email != null && r.Email.Contains(req.Q, StringComparison.OrdinalIgnoreCase)));

        if (req.AccountEnabled.HasValue)
            q = q.Where(r => r.AccountEnabled == req.AccountEnabled.Value);

        var total = q.Count();
        var items = q.OrderBy(r => r.DisplayName)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize).ToList()
            .Select(r => new RegistroEntraIDVisorDto(
                r.EmployeeId, r.DisplayName, r.UserPrincipalName,
                r.Email, r.Department, r.JobTitle,
                r.AccountEnabled, r.Manager,
                r.LastSignInDateTime.HasValue ? r.LastSignInDateTime.Value.ToString("dd/MM/yyyy HH:mm") : null))
            .ToList();

        return new PagedResultado<RegistroEntraIDVisorDto>(total, req.Page, req.PageSize, items);
    }
}
