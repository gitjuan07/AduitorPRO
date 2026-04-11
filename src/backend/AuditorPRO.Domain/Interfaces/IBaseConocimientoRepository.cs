using AuditorPRO.Domain.Entities;

namespace AuditorPRO.Domain.Interfaces;

public interface IBaseConocimientoRepository
{
    Task<List<BaseConocimiento>> ListarAsync(string? dominio, CancellationToken ct = default);
}
