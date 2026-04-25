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

        // ── 1. Purgar LoteCarga antiguos por tipo ────────────────────────────
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

            // Nullear FK en FuenteDatoSimulacion antes de borrar el LoteCarga
            var fuentes = await _db.FuentesDatosSimulacion
                .Where(f => f.LoteCargaId != null && idsAntiguos.Contains(f.LoteCargaId!.Value))
                .ToListAsync(ct);
            foreach (var f in fuentes)
                f.LoteCargaId = null;

            int registrosTipo = 0;

            switch (tipo)
            {
                case "EMPLEADOS":
                    // EmpleadoMaestro puede tener UsuarioSistema.EmpleadoId apuntando a él;
                    // desvinculamos primero para evitar FK violation
                    var empIds = await _db.Empleados
                        .Where(e => e.LoteCargaId != null && idsAntiguos.Contains(e.LoteCargaId!.Value))
                        .Select(e => e.Id)
                        .ToListAsync(ct);
                    if (empIds.Count > 0)
                    {
                        var usersRef = await _db.UsuariosSistema
                            .Where(u => u.EmpleadoId != null && empIds.Contains(u.EmpleadoId!.Value))
                            .ToListAsync(ct);
                        foreach (var u in usersRef)
                            u.EmpleadoId = null;
                    }
                    var emp = await _db.Empleados
                        .Where(e => e.LoteCargaId != null && idsAntiguos.Contains(e.LoteCargaId!.Value))
                        .ToListAsync(ct);
                    _db.Empleados.RemoveRange(emp);
                    registrosTipo = emp.Count;
                    break;

                case "SAP_ROLES":
                    // UsuarioSistema no tiene LoteCargaId — solo se limpian los metadatos del lote
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
            }

            _db.LotesCarga.RemoveRange(antiguos);
            totalLotes += antiguos.Count;
            totalRegistros += registrosTipo;
            detalles[tipo] = registrosTipo;
        }

        // ── 2. Purgar SnapshotEntraID antiguos (no están en LotesCarga) ──────
        var snapIdsAntiguas = await _db.SnapshotsEntraID
            .OrderByDescending(s => s.FechaInstantanea)
            .Skip(1)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (snapIdsAntiguas.Count > 0)
        {
            // RegistrosEntraID tiene cascade desde SnapshotEntraID; los borramos explícitamente
            var regsEntraid = await _db.RegistrosEntraID
                .Where(r => snapIdsAntiguas.Contains(r.SnapshotId))
                .ToListAsync(ct);
            _db.RegistrosEntraID.RemoveRange(regsEntraid);

            var snapsAntiguos = await _db.SnapshotsEntraID
                .Where(s => snapIdsAntiguas.Contains(s.Id))
                .ToListAsync(ct);
            _db.SnapshotsEntraID.RemoveRange(snapsAntiguos);

            int regsBorrados = regsEntraid.Count;
            totalLotes += snapsAntiguos.Count;
            totalRegistros += regsBorrados;
            detalles["SNAPSHOT_ENTRAID"] = regsBorrados;
        }

        await _db.SaveChangesAsync(ct);
        return new PurgarCargasResultado(totalLotes, totalRegistros, detalles);
    }
}
