import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getSimulacion, type SimulacionDetalleDto, type ResultadoControlDto } from '../api/simulaciones';
import { toast } from 'sonner';
import {
  ArrowLeft, CheckCircle2, AlertTriangle, XCircle, HelpCircle,
  Download, RefreshCw, Clock, BarChart2, Shield, ChevronDown, ChevronUp,
  Users, AlertOctagon, FileWarning
} from 'lucide-react';
import api from '../api/client';

// ─────────────────────────────────────────────────────────────────────────────
// Types
// ─────────────────────────────────────────────────────────────────────────────
interface HallazgoDto {
  id: string;
  simulacionId?: string;
  titulo: string;
  descripcion: string;
  criticidad: string;
  estado: string;
  tipoHallazgo?: string;
  usuarioSAP?: string;
  cedula?: string;
  rolAfectado?: string;
  transaccionesAfectadas?: string;
  casoSESuiteRef?: string;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

// ─────────────────────────────────────────────────────────────────────────────
// Constants
// ─────────────────────────────────────────────────────────────────────────────
const TIPO_LABELS: Record<string, string> = {
  R01_SOD:                   'R01 — Segregación de Funciones (SoD)',
  R02_ACCESO_EX_EMPLEADO:    'R02 — Accesos de ex-empleados activos',
  R03_ROL_NO_AUTORIZADO_MATRIZ: 'R03 — Roles no autorizados en Matriz',
  R04_USUARIO_SIN_ENTRA_ID:  'R04 — Usuarios SAP sin cuenta Entra ID',
  R05_ROL_SIN_TRANSACCIONES: 'R05 — Roles sin uso de transacciones',
  SOD:                       'SoD — Segregación de Funciones',
  ACCESO_EX_EMPLEADO:        'Accesos de ex-empleados activos',
  ROL_NO_AUTORIZADO_MATRIZ:  'Roles no autorizados en Matriz',
  USUARIO_SIN_ENTRA_ID:      'Usuarios SAP sin cuenta Entra ID',
  ROL_SIN_TRANSACCIONES:     'Roles sin uso de transacciones',
};

const CRITICIDAD_STYLES: Record<string, { badge: string; row: string }> = {
  CRITICA: { badge: 'bg-red-100 text-red-700 border border-red-200',    row: 'bg-red-50' },
  MEDIA:   { badge: 'bg-yellow-100 text-yellow-700 border border-yellow-200', row: 'bg-yellow-50/40' },
  BAJA:    { badge: 'bg-green-100 text-green-700 border border-green-200',  row: '' },
};

const SEMAFORO_CFG = {
  VERDE:       { icon: <CheckCircle2  size={16} className="text-green-500" />, bg: 'bg-green-50',   border: 'border-green-200',  text: 'text-green-700',  label: 'Cumple' },
  AMARILLO:    { icon: <AlertTriangle size={16} className="text-yellow-500" />, bg: 'bg-yellow-50', border: 'border-yellow-200', text: 'text-yellow-700', label: 'Parcial' },
  ROJO:        { icon: <XCircle       size={16} className="text-red-500" />,   bg: 'bg-red-50',    border: 'border-red-200',    text: 'text-red-700',    label: 'No cumple' },
  NO_EVALUADO: { icon: <HelpCircle    size={16} className="text-gray-400" />, bg: 'bg-gray-50',   border: 'border-gray-200',   text: 'text-gray-500',   label: 'No evaluado' },
};

// ─────────────────────────────────────────────────────────────────────────────
// Gauge
// ─────────────────────────────────────────────────────────────────────────────
function ScoreGauge({ value }: { value: number }) {
  const pct = Math.min(100, Math.max(0, (value / 10) * 100));
  const color = value >= 7 ? '#22c55e' : value >= 4 ? '#eab308' : '#ef4444';
  const r = 52, cx = 60, cy = 60;
  const circumference = Math.PI * r;
  const offset = circumference * (1 - pct / 100);
  return (
    <div className="flex flex-col items-center">
      <svg width={120} height={70} viewBox="0 0 120 70">
        <path d={`M ${cx - r} ${cy} A ${r} ${r} 0 0 1 ${cx + r} ${cy}`}
          fill="none" stroke="#e5e7eb" strokeWidth={12} />
        <path d={`M ${cx - r} ${cy} A ${r} ${r} 0 0 1 ${cx + r} ${cy}`}
          fill="none" stroke={color} strokeWidth={12}
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
          style={{ transition: 'stroke-dashoffset 0.8s ease' }} />
        <text x={cx} y={cy - 4} textAnchor="middle" fontSize={22} fontWeight="bold" fill={color}>{value.toFixed(1)}</text>
        <text x={cx} y={cy + 14} textAnchor="middle" fontSize={10} fill="#6b7280">/10</text>
      </svg>
      <p className="text-xs text-gray-500 -mt-1">Score de Madurez</p>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Control row (legacy ResultadosControl)
// ─────────────────────────────────────────────────────────────────────────────
function ControlRow({ r }: { r: ResultadoControlDto }) {
  const [open, setOpen] = useState(false);
  const cfg = SEMAFORO_CFG[r.semaforo as keyof typeof SEMAFORO_CFG] ?? SEMAFORO_CFG.NO_EVALUADO;
  return (
    <>
      <tr
        className={`cursor-pointer hover:bg-gray-50 transition-colors ${open ? 'bg-gray-50' : ''}`}
        onClick={() => setOpen(o => !o)}
      >
        <td className="px-4 py-3">
          <span className="font-mono text-xs bg-gray-100 px-1.5 py-0.5 rounded text-gray-600">{r.codigoControl}</span>
        </td>
        <td className="px-4 py-3 text-xs text-gray-500">{r.dominio}</td>
        <td className="px-4 py-3 text-sm font-medium text-gray-800 max-w-xs truncate">{r.nombreControl}</td>
        <td className="px-4 py-3">
          <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-semibold border ${cfg.bg} ${cfg.border} ${cfg.text}`}>
            {cfg.icon}{cfg.label}
          </span>
        </td>
        <td className="px-4 py-3">
          <span className={`px-2 py-0.5 rounded text-[11px] font-medium ${CRITICIDAD_STYLES[r.criticidad]?.badge ?? 'bg-gray-100 text-gray-500'}`}>
            {r.criticidad}
          </span>
        </td>
        <td className="px-4 py-3 text-xs text-gray-400 truncate max-w-[180px]">{r.resultadoDetalle ?? '—'}</td>
        <td className="px-4 py-3 text-gray-400">
          {open ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
        </td>
      </tr>
      {open && (r.analisisIa || r.recomendacion || r.resultadoDetalle) && (
        <tr className="bg-blue-50 border-b border-blue-100">
          <td colSpan={7} className="px-6 py-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-xs">
              {r.resultadoDetalle && (
                <div>
                  <p className="font-semibold text-gray-600 mb-1 flex items-center gap-1">
                    <BarChart2 size={12} /> Resultado
                  </p>
                  <p className="text-gray-700 leading-relaxed">{r.resultadoDetalle}</p>
                </div>
              )}
              {r.analisisIa && (
                <div>
                  <p className="font-semibold text-gray-600 mb-1 flex items-center gap-1">
                    <Shield size={12} /> Análisis IA
                  </p>
                  <p className="text-gray-700 leading-relaxed">{r.analisisIa}</p>
                </div>
              )}
              {r.recomendacion && (
                <div>
                  <p className="font-semibold text-blue-600 mb-1">Recomendación</p>
                  <p className="text-gray-700 leading-relaxed">{r.recomendacion}</p>
                </div>
              )}
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Hallazgos panel — grouped by TipoHallazgo
// ─────────────────────────────────────────────────────────────────────────────
interface RuleGroup {
  tipo: string;
  label: string;
  total: number;
  criticos: number;
  medios: number;
  bajos: number;
  items: HallazgoDto[];
}

function HallazgosPanel({ simulacionId }: { simulacionId: string }) {
  const [groups, setGroups]     = useState<RuleGroup[]>([]);
  const [total, setTotal]       = useState(0);
  const [loading, setLoading]   = useState(true);
  const [openGroup, setOpenGroup] = useState<string | null>(null);
  const [filtroCrit, setFiltroCrit]   = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 15;

  useEffect(() => {
    api.get<PagedResult<HallazgoDto>>('/hallazgos', {
      params: { simulacionId, pageSize: 5000 }
    })
      .then(r => {
        const all = r.data.items;
        setTotal(all.length);

        const map = new Map<string, HallazgoDto[]>();
        for (const h of all) {
          const key = h.tipoHallazgo ?? 'SIN_CLASIFICAR';
          if (!map.has(key)) map.set(key, []);
          map.get(key)!.push(h);
        }

        const gs: RuleGroup[] = [];
        for (const [tipo, items] of map.entries()) {
          gs.push({
            tipo,
            label: TIPO_LABELS[tipo] ?? tipo,
            total: items.length,
            criticos: items.filter(h => h.criticidad === 'CRITICA').length,
            medios:   items.filter(h => h.criticidad === 'MEDIA').length,
            bajos:    items.filter(h => h.criticidad === 'BAJA').length,
            items,
          });
        }
        gs.sort((a, b) => b.total - a.total);
        setGroups(gs);
      })
      .catch(() => toast.error('No se pudieron cargar los hallazgos'))
      .finally(() => setLoading(false));
  }, [simulacionId]);

  if (loading) return (
    <div className="py-10 flex justify-center">
      <RefreshCw size={20} className="animate-spin text-gray-400" />
    </div>
  );

  if (total === 0) return (
    <div className="py-16 text-center text-gray-400">
      <FileWarning size={36} className="mx-auto mb-3 text-gray-300" />
      <p>No se encontraron hallazgos para esta simulación.</p>
      <p className="text-xs mt-1">Ejecute el Motor de Control Cruzado para generar hallazgos.</p>
    </div>
  );

  // Filtered items for the detail table
  const activeGroup = openGroup ? groups.find(g => g.tipo === openGroup) : null;
  const detailItems = (activeGroup?.items ?? [])
    .filter(h => !filtroCrit || h.criticidad === filtroCrit);
  const totalPages  = Math.ceil(detailItems.length / PAGE_SIZE);
  const pageItems   = detailItems.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const criticos = groups.reduce((s, g) => s + g.criticos, 0);
  const medios   = groups.reduce((s, g) => s + g.medios, 0);
  const bajos    = groups.reduce((s, g) => s + g.bajos, 0);

  return (
    <div className="space-y-4">
      {/* Summary bar */}
      <div className="grid grid-cols-4 gap-3">
        <div className="bg-gray-50 rounded-xl border border-gray-100 p-4 text-center">
          <p className="text-xs text-gray-500">Total hallazgos</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">{total}</p>
        </div>
        <div className="bg-red-50 rounded-xl border border-red-100 p-4 text-center">
          <p className="text-xs text-red-600">Críticos</p>
          <p className="text-2xl font-bold text-red-700 mt-1">{criticos}</p>
        </div>
        <div className="bg-yellow-50 rounded-xl border border-yellow-100 p-4 text-center">
          <p className="text-xs text-yellow-600">Medios</p>
          <p className="text-2xl font-bold text-yellow-700 mt-1">{medios}</p>
        </div>
        <div className="bg-green-50 rounded-xl border border-green-100 p-4 text-center">
          <p className="text-xs text-green-600">Bajos</p>
          <p className="text-2xl font-bold text-green-700 mt-1">{bajos}</p>
        </div>
      </div>

      {/* Groups */}
      <div className="space-y-2">
        {groups.map(g => (
          <div key={g.tipo} className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
            <button
              className="w-full flex items-center justify-between px-5 py-4 hover:bg-gray-50 transition"
              onClick={() => {
                setOpenGroup(o => o === g.tipo ? null : g.tipo);
                setPage(1);
                setFiltroCrit(null);
              }}
            >
              <div className="flex items-center gap-3">
                <AlertOctagon size={16} className="text-orange-500 shrink-0" />
                <span className="font-semibold text-gray-800 text-sm">{g.label}</span>
                <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded-full">{g.total}</span>
              </div>
              <div className="flex items-center gap-3">
                {g.criticos > 0 && <span className="text-xs px-2 py-0.5 rounded bg-red-100 text-red-700 font-medium">{g.criticos} críticos</span>}
                {g.medios > 0   && <span className="text-xs px-2 py-0.5 rounded bg-yellow-100 text-yellow-700 font-medium">{g.medios} medios</span>}
                {g.bajos > 0    && <span className="text-xs px-2 py-0.5 rounded bg-green-100 text-green-700 font-medium">{g.bajos} bajos</span>}
                {openGroup === g.tipo ? <ChevronUp size={14} className="text-gray-400" /> : <ChevronDown size={14} className="text-gray-400" />}
              </div>
            </button>

            {openGroup === g.tipo && (
              <div className="border-t border-gray-100">
                {/* Criticality filter */}
                <div className="px-5 py-3 flex items-center gap-2 bg-gray-50 border-b border-gray-100">
                  <span className="text-xs text-gray-500">Criticidad:</span>
                  {(['CRITICA','MEDIA','BAJA'] as const).map(c => (
                    <button key={c} onClick={() => { setFiltroCrit(f => f === c ? null : c); setPage(1); }}
                      className={`text-xs px-2.5 py-1 rounded font-medium transition
                        ${filtroCrit === c
                          ? 'bg-blue-600 text-white'
                          : CRITICIDAD_STYLES[c].badge}`}>
                      {c}
                    </button>
                  ))}
                  {filtroCrit && (
                    <button onClick={() => { setFiltroCrit(null); setPage(1); }}
                      className="text-xs text-gray-400 hover:text-gray-700 ml-1">✕ Quitar</button>
                  )}
                  <span className="ml-auto text-xs text-gray-400">{detailItems.length} registros</span>
                </div>

                {/* Table */}
                <div className="overflow-x-auto">
                  <table className="w-full text-xs">
                    <thead className="bg-gray-50 border-b border-gray-100">
                      <tr>
                        <th className="text-left px-4 py-2.5 font-semibold text-gray-500 uppercase tracking-wide whitespace-nowrap">Usuario SAP</th>
                        <th className="text-left px-4 py-2.5 font-semibold text-gray-500 uppercase tracking-wide whitespace-nowrap">Cédula</th>
                        <th className="text-left px-4 py-2.5 font-semibold text-gray-500 uppercase tracking-wide whitespace-nowrap">Rol Afectado</th>
                        <th className="text-left px-4 py-2.5 font-semibold text-gray-500 uppercase tracking-wide whitespace-nowrap">Criticidad</th>
                        <th className="text-left px-4 py-2.5 font-semibold text-gray-500 uppercase tracking-wide">Descripción</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-50">
                      {pageItems.map(h => (
                        <tr key={h.id} className={`hover:bg-blue-50/30 transition ${CRITICIDAD_STYLES[h.criticidad]?.row ?? ''}`}>
                          <td className="px-4 py-2.5 font-mono whitespace-nowrap">{h.usuarioSAP ?? '—'}</td>
                          <td className="px-4 py-2.5 font-mono whitespace-nowrap">{h.cedula ?? '—'}</td>
                          <td className="px-4 py-2.5 max-w-[220px] truncate" title={h.rolAfectado ?? ''}>{h.rolAfectado ?? '—'}</td>
                          <td className="px-4 py-2.5">
                            <span className={`px-2 py-0.5 rounded text-[11px] font-medium ${CRITICIDAD_STYLES[h.criticidad]?.badge ?? 'bg-gray-100 text-gray-500'}`}>
                              {h.criticidad}
                            </span>
                          </td>
                          <td className="px-4 py-2.5 text-gray-600 max-w-[300px] truncate" title={h.descripcion}>{h.descripcion}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                  <div className="flex items-center justify-between px-5 py-3 border-t border-gray-100 bg-gray-50">
                    <button disabled={page === 1} onClick={() => setPage(p => p - 1)}
                      className="text-xs px-3 py-1.5 rounded border border-gray-200 disabled:opacity-40 hover:bg-white transition">
                      ← Anterior
                    </button>
                    <span className="text-xs text-gray-500">Página {page} de {totalPages}</span>
                    <button disabled={page === totalPages} onClick={() => setPage(p => p + 1)}
                      className="text-xs px-3 py-1.5 rounded border border-gray-200 disabled:opacity-40 hover:bg-white transition">
                      Siguiente →
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Page
// ─────────────────────────────────────────────────────────────────────────────
export function SimulacionDetalle() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [sim, setSim] = useState<SimulacionDetalleDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'hallazgos' | 'controles'>('hallazgos');
  const [filtroDominio, setFiltroDominio] = useState<string | null>(null);
  const [filtroSemaforo, setFiltroSemaforo] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getSimulacion(id)
      .then(setSim)
      .catch(() => { toast.error('No se pudo cargar la simulación'); navigate('/simulaciones'); })
      .finally(() => setLoading(false));
  }, [id]);

  const safeFilename = (ext: string) => {
    const base = sim?.nombre
      ? sim.nombre.replace(/[^a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s_-]/g, '').replace(/\s+/g, '_').slice(0, 60)
      : id;
    return `${base}.${ext}`;
  };

  const exportarWord = async () => {
    setExporting(true);
    try {
      const res = await api.get(`/exportar/simulacion/${id}/word`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement('a'); a.href = url; a.download = safeFilename('docx'); a.click();
      URL.revokeObjectURL(url);
    } catch { toast.error('Error al exportar Word'); }
    finally { setExporting(false); }
  };

  const exportarPpt = async () => {
    setExporting(true);
    try {
      const res = await api.get(`/exportar/simulacion/${id}/ppt`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement('a'); a.href = url; a.download = safeFilename('pptx'); a.click();
      URL.revokeObjectURL(url);
    } catch { toast.error('Error al exportar PPT'); }
    finally { setExporting(false); }
  };

  if (loading) return (
    <div className="p-6 flex items-center justify-center min-h-96">
      <RefreshCw size={24} className="animate-spin text-gray-400" />
    </div>
  );
  if (!sim) return null;

  const dominios = [...new Set(sim.resultados.map(r => r.dominio).filter(Boolean))];
  const resultados = sim.resultados.filter(r => {
    if (filtroDominio && r.dominio !== filtroDominio) return false;
    if (filtroSemaforo && r.semaforo !== filtroSemaforo) return false;
    return true;
  });

  const verde    = sim.resultados.filter(r => r.semaforo === 'VERDE').length;
  const amarillo = sim.resultados.filter(r => r.semaforo === 'AMARILLO').length;
  const rojo     = sim.resultados.filter(r => r.semaforo === 'ROJO').length;
  const total    = sim.resultados.length;

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="bg-white rounded-xl border border-gray-100 shadow-sm px-5 py-4">
        <div className="flex items-start justify-between">
          <div className="flex items-start gap-3">
            <button onClick={() => navigate('/simulaciones')}
              className="mt-1 text-gray-400 hover:text-gray-700 transition flex items-center gap-1 text-xs">
              <ArrowLeft size={15} />
              <span className="hidden sm:inline text-gray-400">Simulaciones</span>
            </button>
            <div className="border-l border-gray-200 pl-3">
              <div className="flex items-center gap-2 mb-0.5">
                <span className={`px-2 py-0.5 rounded-full text-[11px] font-semibold ${
                  sim.estado === 'COMPLETADA' ? 'bg-green-100 text-green-700' :
                  sim.estado === 'EN_PROCESO' ? 'bg-blue-100 text-blue-700' :
                  sim.estado === 'ERROR'      ? 'bg-red-100 text-red-700' :
                  'bg-gray-100 text-gray-600'}`}>{sim.estado}</span>
                <span className="text-xs text-gray-400 flex items-center gap-1">
                  <Clock size={10} /> Período: {sim.periodoInicio} — {sim.periodoFin}
                </span>
              </div>
              <h1 className="text-xl font-bold text-gray-900 leading-tight">{sim.nombre}</h1>
              {sim.completadaAt && (
                <p className="text-[11px] text-gray-400 mt-0.5">
                  Completada el {new Date(sim.completadaAt).toLocaleString('es-CR', { dateStyle: 'long', timeStyle: 'short' })}
                </p>
              )}
            </div>
          </div>
          <div className="flex items-center gap-2 shrink-0 ml-4">
            <button onClick={exportarWord} disabled={exporting}
              className="flex items-center gap-1.5 text-xs px-3 py-1.5 border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition">
              <Download size={13} /> Word
            </button>
            <button onClick={exportarPpt} disabled={exporting}
              className="flex items-center gap-1.5 text-xs px-3 py-1.5 border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition">
              <Download size={13} /> PPT
            </button>
          </div>
        </div>
      </div>

      {/* KPI cards */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        {sim.scoreMadurez != null && (
          <div className="col-span-2 bg-white rounded-xl border border-gray-100 shadow-sm p-4 flex items-center justify-center">
            <ScoreGauge value={Number(sim.scoreMadurez)} />
          </div>
        )}
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4 text-center">
          <p className="text-xs text-gray-500">Cumplimiento</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">
            {sim.porcentajeCumplimiento != null ? `${Number(sim.porcentajeCumplimiento).toFixed(1)}%` : '—'}
          </p>
        </div>
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4 flex items-center gap-3">
          <Users size={20} className="text-blue-400 shrink-0" />
          <div>
            <p className="text-xs text-gray-500">Período</p>
            <p className="text-xs font-medium text-gray-700">{sim.periodoInicio}</p>
            <p className="text-xs font-medium text-gray-700">{sim.periodoFin}</p>
          </div>
        </div>
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4 flex items-center gap-3">
          <Shield size={20} className="text-purple-400 shrink-0" />
          <div>
            <p className="text-xs text-gray-500">Estado</p>
            <p className="text-sm font-bold text-gray-700">{sim.estado}</p>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-gray-200">
        <button
          onClick={() => setActiveTab('hallazgos')}
          className={`px-4 py-2.5 text-sm font-medium border-b-2 transition -mb-px ${
            activeTab === 'hallazgos'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          }`}
        >
          Hallazgos de Control Cruzado
        </button>
        {total > 0 && (
          <button
            onClick={() => setActiveTab('controles')}
            className={`px-4 py-2.5 text-sm font-medium border-b-2 transition -mb-px ${
              activeTab === 'controles'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            Controles ({total})
          </button>
        )}
      </div>

      {/* Tab content */}
      {activeTab === 'hallazgos' && id && (
        <HallazgosPanel simulacionId={id} />
      )}

      {activeTab === 'controles' && (
        <>
          {/* Barra visual semáforo */}
          {total > 0 && (
            <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4">
              <p className="text-xs font-medium text-gray-500 mb-2">Distribución de resultados</p>
              <div className="flex rounded-full overflow-hidden h-3">
                {verde    > 0 && <div className="bg-green-500 transition-all"  style={{ width: `${(verde    / total) * 100}%` }} />}
                {amarillo > 0 && <div className="bg-yellow-400 transition-all" style={{ width: `${(amarillo / total) * 100}%` }} />}
                {rojo     > 0 && <div className="bg-red-500 transition-all"    style={{ width: `${(rojo     / total) * 100}%` }} />}
              </div>
              <div className="flex gap-4 mt-2 text-[11px] text-gray-500">
                <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-green-500 inline-block" />{Math.round((verde/total)*100)}% cumple</span>
                <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-yellow-400 inline-block" />{Math.round((amarillo/total)*100)}% parcial</span>
                <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-red-500 inline-block" />{Math.round((rojo/total)*100)}% no cumple</span>
              </div>
            </div>
          )}

          {/* Filtros */}
          {total > 0 && (
            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-xs text-gray-500 font-medium">Dominio:</span>
              <button onClick={() => setFiltroDominio(null)}
                className={`px-2.5 py-1 rounded-lg text-xs font-medium transition ${!filtroDominio ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'}`}>
                Todos
              </button>
              {dominios.map(d => (
                <button key={d} onClick={() => setFiltroDominio(f => f === d ? null : d)}
                  className={`px-2.5 py-1 rounded-lg text-xs font-medium transition ${filtroDominio === d ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'}`}>
                  {d}
                </button>
              ))}
              <span className="ml-2 text-xs text-gray-500 font-medium">Semáforo:</span>
              {(['VERDE','AMARILLO','ROJO'] as const).map(s => {
                const cfg = SEMAFORO_CFG[s];
                return (
                  <button key={s} onClick={() => setFiltroSemaforo(f => f === s ? null : s)}
                    className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-medium border transition ${
                      filtroSemaforo === s ? 'bg-blue-600 text-white border-blue-600' :
                      `${cfg.bg} ${cfg.text} ${cfg.border} hover:opacity-80`}`}>
                    {cfg.icon}{cfg.label}
                  </button>
                );
              })}
            </div>
          )}

          {/* Tabla de controles */}
          <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
            {resultados.length === 0 ? (
              <div className="py-16 text-center text-gray-400">
                <Shield size={36} className="mx-auto mb-3 text-gray-300" />
                <p>No hay resultados de controles en esta simulación</p>
                {(filtroDominio || filtroSemaforo) && (
                  <button className="mt-3 text-xs text-blue-600 hover:underline"
                    onClick={() => { setFiltroDominio(null); setFiltroSemaforo(null); }}>
                    Quitar filtros
                  </button>
                )}
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-100">
                  <tr>
                    <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Código</th>
                    <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Dominio</th>
                    <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Control</th>
                    <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Resultado</th>
                    <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Criticidad</th>
                    <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Detalle</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {resultados.map(r => <ControlRow key={r.id} r={r} />)}
                </tbody>
              </table>
            )}
          </div>
        </>
      )}
    </div>
  );
}
