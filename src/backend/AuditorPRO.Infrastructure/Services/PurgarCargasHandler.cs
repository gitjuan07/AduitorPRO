using AuditorPRO.Application.Features.Cargas;
using AuditorPRO.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuditorPRO.Infrastructure.Services;

public class PurgarCargasAntiguasHandler : IRequestHandler<PurgarCargasAntiguasCommand, PurgarCargasResultado>
{
    private readonly AppDbContext _db;
    public PurgarCargasAntiguasHandler(AppDbContext db) => _db = db;

    public async Task<PurgarCargasResultado> Handle(PurgarCargasAntiguasCommand _, CancellationToken ct)
    {
        var detalles = new Dictionary<string, int>();
        int totalLotes = 0;
        int totalRegistros = 0;

        var tipos = await _db.LotesCarga
            .Select(l => l.TipoCarga)
            .Distinct()
            .ToListAsync(ct);

        foreach (var tipo in tipos)
        {
            var lotes = await _db.LotesCarga
                .Where(l => l.TipoCarga == tipo)
                .OrderByDescending(l => l.FechaCarga)
                .ToListAsync(ct);

            if (lotes.Count <= 1) continue;

            var antiguos = lotes.Skip(1).ToList();
            var idsAntiguos = antiguos.Select(l => l.Id).ToHashSet();
            int registrosTipo = 0;

            switch (tipo)
            {
                case "EMPLEADOS":
                    var emp = await _db.Empleados
                        .Where(e => e.LoteCargaId != null && idsAntiguos.Contains(e.LoteCargaId!.Value))
                        .ToListAsync(ct);
                    _db.Empleados.RemoveRange(emp);
                    registrosTipo = emp.Count;
                    break;

                case "SAP_ROLES":
                    // UsuarioSistema no tiene LoteCargaId — solo se eliminan los metadatos del lote
                    // Los registros de usuario son actualizados in-place en cada carga
                    break;

                case "MATRIZ_PUESTOS":
                    var mtz = await _db.MatrizPuestosSAP
                        .Where(m => m.LoteCargaId != null && idsAntiguos.Contains(m.LoteCargaId!.Value))
                        .ToListAsync(ct);
                    _db.MatrizPuestosSAP.RemoveRange(mtz);
                    registrosTipo = mtz.Count;
                    break;

                case "CASOS_SESUITE":
                    var cas = await _db.CasosSESuite
                        .Where(c => c.LoteCargaId != null && idsAntiguos.Contains(c.LoteCargaId!.Value))
                        .ToListAsync(ct);
                    _db.CasosSESuite.RemoveRange(cas);
                    registrosTipo = cas.Count;
                    break;

                case "SNAPSHOT_ENTRAID":
                    // SnapshotEntraID no usa LoteCargaId — sus IDs son los propios Guids
                    var snapIds = await _db.SnapshotsEntraID
                        .OrderByDescending(s => s.FechaInstantanea)
                        .Skip(1)
                        .Select(s => s.Id)
                        .ToListAsync(ct);
                    if (snapIds.Count > 0)
                    {
                        var regs = await _db.RegistrosEntraID
                            .Where(r => snapIds.Contains(r.SnapshotId))
                            .ToListAsync(ct);
                        _db.RegistrosEntraID.RemoveRange(regs);
                        registrosTipo = regs.Count;
                        var snaps = await _db.SnapshotsEntraID
                            .Where(s => snapIds.Contains(s.Id))
                            .ToListAsync(ct);
                        _db.SnapshotsEntraID.RemoveRange(snaps);
                        // No suma a totalLotes aquí porque los snapshots no son LoteCarga
                        totalLotes += snaps.Count;
                    }
                    // Saltar el bloque genérico de RemoveRange(antiguos) para este tipo
                    totalRegistros += registrosTipo;
                    if (registrosTipo > 0) detalles[tipo] = registrosTipo;
                    continue;
            }

            _db.LotesCarga.RemoveRange(antiguos);
            totalLotes += antiguos.Count;
            totalRegistros += registrosTipo;
            if (registrosTipo > 0 || antiguos.Count > 0)
                detalles[tipo] = registrosTipo;
        }

        await _db.SaveChangesAsync(ct);
        return new PurgarCargasResultado(totalLotes, totalRegistros, detalles);
    }
}
