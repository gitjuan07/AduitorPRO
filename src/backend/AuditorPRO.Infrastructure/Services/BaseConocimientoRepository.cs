using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Interfaces;
using AuditorPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuditorPRO.Infrastructure.Services;

public class BaseConocimientoRepository : IBaseConocimientoRepository
{
    private readonly AppDbContext _db;
    public BaseConocimientoRepository(AppDbContext db) => _db = db;

    public async Task<List<BaseConocimiento>> ListarAsync(string? dominio, CancellationToken ct = default)
    {
        var query = _db.BaseConocimiento.Where(b => !b.IsDeleted && b.Estado == "PROCESADO");
        if (!string.IsNullOrWhiteSpace(dominio))
            query = query.Where(b => b.DominioDetectado == dominio);
        return await query.OrderByDescending(b => b.CreadoAt).ToListAsync(ct);
    }
}
