import { useState, useEffect } from 'react';
import api from '../api/client';
import { toast } from 'sonner';
import { FileText, AlertTriangle, CheckCircle2, Clock, Search, Filter } from 'lucide-react';

interface PoliticaDto {
  id: string;
  titulo: string;
  codigo: string;
  descripcion?: string;
  estado: string;
  normaReferencia?: string;
  responsable?: string;
  fechaVigencia?: string;
  fechaRevision?: string;
  version: number;
  documentoUrl?: string;
}

const ESTADO_CONFIG: Record<string, { label: string; color: string; bg: string; icon: React.ReactNode }> = {
  VIGENTE:  { label: 'Vigente',     color: 'text-green-700',  bg: 'bg-green-50',  icon: <CheckCircle2 size={14} className="text-green-500" /> },
  REVISION: { label: 'En revisión', color: 'text-yellow-700', bg: 'bg-yellow-50', icon: <Clock size={14} className="text-yellow-500" /> },
  OBSOLETA: { label: 'Obsoleta',    color: 'text-gray-500',   bg: 'bg-gray-100',  icon: <AlertTriangle size={14} className="text-gray-400" /> },
  BORRADOR: { label: 'Borrador',    color: 'text-blue-700',   bg: 'bg-blue-50',   icon: <FileText size={14} className="text-blue-500" /> },
};

export function Politicas() {
  const [politicas, setPoliticas] = useState<PoliticaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [filtroEstado, setFiltroEstado] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    api.get<{ items: PoliticaDto[] }>('/politicas')
      .then(r => setPoliticas(r.data.items))
      .catch(() => toast.error('Error al cargar políticas'))
      .finally(() => setLoading(false));
  }, []);

  const filtered = politicas.filter(p => {
    const matchSearch = !search ||
      p.titulo.toLowerCase().includes(search.toLowerCase()) ||
      p.codigo.toLowerCase().includes(search.toLowerCase());
    const matchEstado = !filtroEstado || p.estado === filtroEstado;
    return matchSearch && matchEstado;
  });

  const now = new Date();
  const stats = {
    total:    politicas.length,
    vigentes: politicas.filter(p => p.estado === 'VIGENTE').length,
    revision: politicas.filter(p => p.estado === 'REVISION').length,
    obsoletas:politicas.filter(p => p.estado === 'OBSOLETA').length,
    vencidas: politicas.filter(p => p.fechaVigencia && new Date(p.fechaVigencia) < now).length,
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Políticas y Procedimientos</h1>
        <p className="text-sm text-gray-500 mt-0.5">Catálogo y control de vigencia documental</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-5 gap-4 mb-6">
        {[
          { label: 'Total',      value: stats.total,     color: 'text-gray-700',   bg: 'bg-white' },
          { label: 'Vigentes',   value: stats.vigentes,  color: 'text-green-700',  bg: 'bg-green-50' },
          { label: 'En revisión',value: stats.revision,  color: 'text-yellow-700', bg: 'bg-yellow-50' },
          { label: 'Obsoletas',  value: stats.obsoletas, color: 'text-gray-500',   bg: 'bg-gray-100' },
          { label: 'Vencidas',   value: stats.vencidas,  color: 'text-red-700',    bg: 'bg-red-50' },
        ].map(s => (
          <div key={s.label} className={`rounded-xl p-4 shadow-sm border border-gray-100 ${s.bg}`}>
            <p className="text-xs text-gray-500">{s.label}</p>
            <p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p>
          </div>
        ))}
      </div>

      {/* Filtros */}
      <div className="flex items-center gap-3 mb-4 flex-wrap">
        <div className="relative flex-1 max-w-md">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input type="text" placeholder="Buscar por título o código..." value={search}
            onChange={e => setSearch(e.target.value)}
            className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
        </div>
        <Filter size={15} className="text-gray-400" />
        {['VIGENTE', 'REVISION', 'BORRADOR', 'OBSOLETA'].map(e => (
          <button key={e} onClick={() => setFiltroEstado(f => f === e ? null : e)}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium transition ${
              filtroEstado === e ? 'bg-blue-600 text-white' :
              `${ESTADO_CONFIG[e]?.bg} ${ESTADO_CONFIG[e]?.color} hover:opacity-80`
            }`}>
            {ESTADO_CONFIG[e]?.label ?? e}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Cargando políticas...</div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border border-gray-100">
          <FileText size={40} className="mx-auto text-gray-300 mb-3" />
          <p className="text-gray-500 font-medium">
            {search || filtroEstado ? 'Sin resultados para el filtro aplicado' : 'No hay políticas registradas'}
          </p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-100">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Código</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Política / Procedimiento</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Estado</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Responsable</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Vigencia</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Versión</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {filtered.map(p => {
                const cfg = ESTADO_CONFIG[p.estado] ?? ESTADO_CONFIG.BORRADOR;
                const isVencida = p.fechaVigencia && new Date(p.fechaVigencia) < now;
                return (
                  <tr key={p.id} className={`hover:bg-gray-50 transition-colors ${isVencida ? 'bg-red-50' : ''}`}>
                    <td className="px-4 py-3">
                      <span className="font-mono text-xs bg-gray-100 px-1.5 py-0.5 rounded text-gray-600">{p.codigo}</span>
                    </td>
                    <td className="px-4 py-3 max-w-sm">
                      <p className="font-medium text-gray-800">{p.titulo}</p>
                      {p.normaReferencia && <p className="text-xs text-gray-400 mt-0.5">{p.normaReferencia}</p>}
                    </td>
                    <td className="px-4 py-3">
                      <div className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium ${cfg.bg} ${cfg.color}`}>
                        {cfg.icon}{cfg.label}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-gray-500 text-xs truncate max-w-[150px]">{p.responsable ?? '—'}</td>
                    <td className="px-4 py-3">
                      {p.fechaVigencia ? (
                        <span className={`text-xs ${isVencida ? 'text-red-600 font-semibold' : 'text-gray-500'}`}>
                          {p.fechaVigencia}
                          {isVencida && <span className="ml-1 text-red-500">VENCIDA</span>}
                        </span>
                      ) : '—'}
                    </td>
                    <td className="px-4 py-3 text-gray-500 text-xs">v{p.version}</td>
                    <td className="px-4 py-3">
                      {p.documentoUrl && (
                        <a href={p.documentoUrl} target="_blank" rel="noopener noreferrer"
                          className="text-xs text-blue-600 hover:underline">Ver doc</a>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
