import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getSimulacion, type SimulacionDetalleDto, type ResultadoControlDto } from '../api/simulaciones';
import { toast } from 'sonner';
import {
  ArrowLeft, CheckCircle2, AlertTriangle, XCircle, HelpCircle,
  Download, RefreshCw, Clock, BarChart2, Shield, ChevronDown, ChevronUp
} from 'lucide-react';
import api from '../api/client';

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────
const SEMAFORO_CFG = {
  VERDE:       { icon: <CheckCircle2  size={16} className="text-green-500" />, bg: 'bg-green-50',   border: 'border-green-200',  text: 'text-green-700',  label: 'Cumple' },
  AMARILLO:    { icon: <AlertTriangle size={16} className="text-yellow-500" />, bg: 'bg-yellow-50', border: 'border-yellow-200', text: 'text-yellow-700', label: 'Parcial' },
  ROJO:        { icon: <XCircle       size={16} className="text-red-500" />,   bg: 'bg-red-50',    border: 'border-red-200',    text: 'text-red-700',    label: 'No cumple' },
  NO_EVALUADO: { icon: <HelpCircle    size={16} className="text-gray-400" />, bg: 'bg-gray-50',   border: 'border-gray-200',   text: 'text-gray-500',   label: 'No evaluado' },
};

const CRITICIDAD_BADGE: Record<string, string> = {
  CRITICA: 'bg-red-100 text-red-700',
  MEDIA:   'bg-yellow-100 text-yellow-700',
  BAJA:    'bg-green-100 text-green-700',
};

function ScoreGauge({ value }: { value: number }) {
  const pct = Math.min(100, Math.max(0, (value / 10) * 100));
  const color = value >= 7 ? '#22c55e' : value >= 4 ? '#eab308' : '#ef4444';
  const r = 52, cx = 60, cy = 60;
  const circumference = Math.PI * r; // half-circle
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
// Control row — expandable
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
          <span className={`px-2 py-0.5 rounded text-[11px] font-medium ${CRITICIDAD_BADGE[r.criticidad] ?? 'bg-gray-100 text-gray-500'}`}>
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
// Page
// ─────────────────────────────────────────────────────────────────────────────
export function SimulacionDetalle() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [sim, setSim] = useState<SimulacionDetalleDto | null>(null);
  const [loading, setLoading] = useState(true);
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

  const exportarWord = async () => {
    setExporting(true);
    try {
      const res = await api.get(`/exportar/simulacion/${id}/word`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement('a'); a.href = url; a.download = `informe_${id}.docx`; a.click();
      URL.revokeObjectURL(url);
    } catch { toast.error('Error al exportar'); }
    finally { setExporting(false); }
  };

  const exportarPpt = async () => {
    setExporting(true);
    try {
      const res = await api.get(`/exportar/simulacion/${id}/ppt`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement('a'); a.href = url; a.download = `resumen_${id}.pptx`; a.click();
      URL.revokeObjectURL(url);
    } catch { toast.error('Error al exportar'); }
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
      <div className="flex items-start justify-between">
        <div className="flex items-start gap-3">
          <button onClick={() => navigate('/simulaciones')} className="mt-1 text-gray-400 hover:text-gray-700 transition">
            <ArrowLeft size={18} />
          </button>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{sim.nombre}</h1>
            <div className="flex items-center gap-3 mt-1 text-xs text-gray-500">
              <span className={`px-2 py-0.5 rounded-full font-medium ${
                sim.estado === 'COMPLETADA' ? 'bg-green-100 text-green-700' :
                sim.estado === 'EN_PROCESO' ? 'bg-blue-100 text-blue-700' :
                sim.estado === 'ERROR'      ? 'bg-red-100 text-red-700' :
                'bg-gray-100 text-gray-600'}`}>{sim.estado}</span>
              <span className="flex items-center gap-1"><Clock size={11} />{sim.periodoInicio} — {sim.periodoFin}</span>
              {sim.completadaAt && <span>Completada: {new Date(sim.completadaAt).toLocaleString('es-CR')}</span>}
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2">
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

      {/* KPI cards */}
      <div className="grid grid-cols-2 md:grid-cols-6 gap-4">
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
        <div className="bg-green-50 rounded-xl border border-green-100 shadow-sm p-4 text-center">
          <p className="text-xs text-green-600">Verdes</p>
          <p className="text-2xl font-bold text-green-700 mt-1">{verde}</p>
        </div>
        <div className="bg-yellow-50 rounded-xl border border-yellow-100 shadow-sm p-4 text-center">
          <p className="text-xs text-yellow-600">Parciales</p>
          <p className="text-2xl font-bold text-yellow-700 mt-1">{amarillo}</p>
        </div>
        <div className="bg-red-50 rounded-xl border border-red-100 shadow-sm p-4 text-center">
          <p className="text-xs text-red-600">Rojos</p>
          <p className="text-2xl font-bold text-red-700 mt-1">{rojo}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4 text-center">
          <p className="text-xs text-gray-500">Controles</p>
          <p className="text-2xl font-bold text-gray-700 mt-1">{total}</p>
        </div>
      </div>

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

      {/* Tabla de resultados */}
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
    </div>
  );
}
