import { useState, useEffect } from 'react';
import { planesAccionApi, type PlanAccionDto } from '../api/planesAccion';
import { toast } from 'sonner';
import { CheckCircle2, Clock, AlertTriangle, XCircle, Filter } from 'lucide-react';

const ESTADO_CONFIG: Record<string, { label: string; color: string; bg: string }> = {
  ABIERTO:    { label: 'Abierto',    color: 'text-red-700',    bg: 'bg-red-50' },
  EN_PROCESO: { label: 'En proceso', color: 'text-yellow-700', bg: 'bg-yellow-50' },
  RESUELTO:   { label: 'Resuelto',   color: 'text-blue-700',   bg: 'bg-blue-50' },
  CERRADO:    { label: 'Cerrado',    color: 'text-green-700',  bg: 'bg-green-50' },
  ACEPTADO:   { label: 'Aceptado',   color: 'text-purple-700', bg: 'bg-purple-50' },
};

const CRIT_CONFIG: Record<string, { color: string; bg: string }> = {
  CRITICA: { color: 'text-red-700',    bg: 'bg-red-100' },
  MEDIA:   { color: 'text-yellow-700', bg: 'bg-yellow-100' },
  BAJA:    { color: 'text-gray-600',   bg: 'bg-gray-100' },
};

const ESTADOS = ['ABIERTO', 'EN_PROCESO', 'RESUELTO', 'CERRADO'];

export function PlanesAccion() {
  const [plans, setPlans] = useState<PlanAccionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filtroEstado, setFiltroEstado] = useState<string | null>(null);
  const [soloVencidos, setSoloVencidos] = useState(false);

  const load = () => {
    setLoading(true);
    planesAccionApi.getAll({ pageSize: 100 })
      .then(r => setPlans(r.items))
      .catch(() => toast.error('Error al cargar planes de acción'))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const handleCambiarEstado = async (hallazgoId: string, nuevoEstado: string) => {
    try {
      await planesAccionApi.actualizarEstatus({ hallazgoId, nuevoEstado });
      toast.success('Estado actualizado');
      load();
    } catch {
      toast.error('Error al actualizar el estado');
    }
  };

  const byEstado = ESTADOS.reduce<Record<string, PlanAccionDto[]>>((acc, e) => {
    acc[e] = plans.filter(p => p.estado === e);
    return acc;
  }, {});

  const totalVencidos = plans.filter(p => p.esVencido).length;
  const displayed = filtroEstado
    ? byEstado[filtroEstado] ?? []
    : soloVencidos
    ? plans.filter(p => p.esVencido)
    : plans;

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Planes de Acción</h1>
          <p className="text-sm text-gray-500 mt-0.5">Seguimiento de compromisos por hallazgo</p>
        </div>
        {totalVencidos > 0 && (
          <div className="flex items-center gap-2 bg-red-50 text-red-700 px-3 py-1.5 rounded-lg text-sm font-medium border border-red-200">
            <AlertTriangle size={15} />
            {totalVencidos} plan{totalVencidos > 1 ? 'es' : ''} vencido{totalVencidos > 1 ? 's' : ''}
          </div>
        )}
      </div>

      {/* Filtros */}
      <div className="flex items-center gap-3 mb-6 flex-wrap">
        <Filter size={15} className="text-gray-400" />
        <button
          onClick={() => { setFiltroEstado(null); setSoloVencidos(false); }}
          className={`px-3 py-1.5 rounded-lg text-xs font-medium transition ${
            !filtroEstado && !soloVencidos ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
          }`}
        >
          Todos ({plans.length})
        </button>
        {ESTADOS.map(e => (
          <button key={e} onClick={() => { setFiltroEstado(e); setSoloVencidos(false); }}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium transition ${
              filtroEstado === e ? 'bg-blue-600 text-white' :
              `${ESTADO_CONFIG[e]?.bg ?? 'bg-gray-100'} ${ESTADO_CONFIG[e]?.color ?? 'text-gray-600'} hover:opacity-80`
            }`}>
            {ESTADO_CONFIG[e]?.label ?? e} ({byEstado[e]?.length ?? 0})
          </button>
        ))}
        <button onClick={() => { setSoloVencidos(v => !v); setFiltroEstado(null); }}
          className={`px-3 py-1.5 rounded-lg text-xs font-medium transition ${
            soloVencidos ? 'bg-red-600 text-white' : 'bg-red-50 text-red-600 hover:bg-red-100'
          }`}>
          ⚠ Vencidos ({totalVencidos})
        </button>
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Cargando planes de acción...</div>
      ) : !filtroEstado && !soloVencidos ? (
        /* Vista Kanban */
        <div className="grid grid-cols-4 gap-4">
          {ESTADOS.map(estado => (
            <div key={estado} className="bg-gray-50 rounded-xl p-3 min-h-[400px]">
              <div className="flex items-center gap-2 mb-3">
                <span className={`text-xs font-bold uppercase tracking-wide ${ESTADO_CONFIG[estado]?.color}`}>
                  {ESTADO_CONFIG[estado]?.label}
                </span>
                <span className="ml-auto bg-white text-gray-600 text-xs font-semibold rounded-full px-2 py-0.5 border border-gray-200">
                  {byEstado[estado]?.length ?? 0}
                </span>
              </div>
              <div className="space-y-2">
                {(byEstado[estado] ?? []).length === 0 && (
                  <p className="text-center text-xs text-gray-400 py-8">Sin planes</p>
                )}
                {(byEstado[estado] ?? []).map(p => (
                  <KanbanCard key={p.hallazgoId} plan={p} onCambiarEstado={handleCambiarEstado} />
                ))}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <PlanesTable plans={displayed} onCambiarEstado={handleCambiarEstado} />
      )}
    </div>
  );
}

function KanbanCard({ plan, onCambiarEstado }: { plan: PlanAccionDto; onCambiarEstado: (id: string, e: string) => void }) {
  const crit = CRIT_CONFIG[plan.criticidad] ?? CRIT_CONFIG.BAJA;
  const nextEstado = ({ ABIERTO: 'EN_PROCESO', EN_PROCESO: 'RESUELTO', RESUELTO: 'CERRADO' } as Record<string, string>)[plan.estado];

  return (
    <div className={`bg-white rounded-lg p-3 shadow-sm border ${plan.esVencido ? 'border-red-300' : 'border-gray-100'}`}>
      <div className="flex items-start justify-between gap-2 mb-2">
        <p className="text-xs font-semibold text-gray-800 line-clamp-2">{plan.titulo}</p>
        <span className={`shrink-0 text-[10px] font-bold px-1.5 py-0.5 rounded ${crit.bg} ${crit.color}`}>
          {plan.criticidad}
        </span>
      </div>
      {plan.responsable && <p className="text-[11px] text-gray-500 mb-1 truncate">👤 {plan.responsable}</p>}
      {plan.fechaCompromiso && (
        <div className={`flex items-center gap-1 text-[11px] mb-2 ${plan.esVencido ? 'text-red-600 font-semibold' : 'text-gray-500'}`}>
          <Clock size={11} />
          {plan.fechaCompromiso}
          {plan.esVencido && <span className="ml-1">• VENCIDO</span>}
        </div>
      )}
      {nextEstado && (
        <button onClick={() => onCambiarEstado(plan.hallazgoId, nextEstado)}
          className="w-full text-[11px] font-medium text-blue-600 hover:bg-blue-50 py-1 rounded transition text-center">
          → {ESTADO_CONFIG[nextEstado]?.label}
        </button>
      )}
      {plan.estado === 'RESUELTO' && (
        <button onClick={() => onCambiarEstado(plan.hallazgoId, 'CERRADO')}
          className="w-full text-[11px] font-medium text-green-600 hover:bg-green-50 py-1 rounded transition flex items-center justify-center gap-1">
          <CheckCircle2 size={11} /> Cerrar
        </button>
      )}
    </div>
  );
}

function PlanesTable({ plans, onCambiarEstado }: { plans: PlanAccionDto[]; onCambiarEstado: (id: string, e: string) => void }) {
  if (!plans.length) return (
    <div className="text-center py-16 bg-white rounded-xl border border-gray-100">
      <XCircle size={40} className="mx-auto text-gray-300 mb-3" />
      <p className="text-gray-500">No hay planes de acción en esta categoría</p>
    </div>
  );
  return (
    <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-gray-50 border-b border-gray-100">
          <tr>
            <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Hallazgo</th>
            <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Criticidad</th>
            <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Responsable</th>
            <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Compromiso</th>
            <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Estado</th>
            <th className="px-4 py-3" />
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-50">
          {plans.map(p => {
            const cfg = ESTADO_CONFIG[p.estado] ?? { label: p.estado, color: 'text-gray-600', bg: 'bg-gray-50' };
            const crit = CRIT_CONFIG[p.criticidad] ?? CRIT_CONFIG.BAJA;
            const nextEstado = ({ ABIERTO: 'EN_PROCESO', EN_PROCESO: 'RESUELTO', RESUELTO: 'CERRADO' } as Record<string, string>)[p.estado];
            return (
              <tr key={p.hallazgoId} className={`hover:bg-gray-50 transition-colors ${p.esVencido ? 'bg-red-50' : ''}`}>
                <td className="px-4 py-3 max-w-xs">
                  <p className="font-medium text-gray-800 truncate">{p.titulo}</p>
                  {p.planAccion && <p className="text-xs text-gray-400 truncate mt-0.5">{p.planAccion}</p>}
                </td>
                <td className="px-4 py-3">
                  <span className={`text-xs font-bold px-2 py-0.5 rounded ${crit.bg} ${crit.color}`}>{p.criticidad}</span>
                </td>
                <td className="px-4 py-3 text-gray-600 text-xs">{p.responsable ?? '—'}</td>
                <td className="px-4 py-3">
                  <span className={`text-xs ${p.esVencido ? 'text-red-600 font-semibold' : 'text-gray-600'}`}>
                    {p.fechaCompromiso ?? '—'}
                  </span>
                  {p.esVencido && <span className="ml-1 text-red-500 text-[10px]">VENCIDO</span>}
                </td>
                <td className="px-4 py-3">
                  <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${cfg.bg} ${cfg.color}`}>{cfg.label}</span>
                </td>
                <td className="px-4 py-3">
                  {nextEstado && (
                    <button onClick={() => onCambiarEstado(p.hallazgoId, nextEstado)}
                      className="text-xs text-blue-600 hover:underline">
                      → {ESTADO_CONFIG[nextEstado]?.label}
                    </button>
                  )}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
