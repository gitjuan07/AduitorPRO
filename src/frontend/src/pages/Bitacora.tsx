import { useState, useEffect } from 'react';
import { bitacoraApi, type BitacoraEventoDto } from '../api/bitacora';
import { toast } from 'sonner';
import { BookOpen, CheckCircle2, XCircle, Search, Filter, RefreshCw } from 'lucide-react';

const ACCION_COLORS: Record<string, string> = {
  CREAR:               'bg-green-100 text-green-700',
  ACTUALIZAR:          'bg-blue-100 text-blue-700',
  ELIMINAR:            'bg-red-100 text-red-700',
  LEER:                'bg-gray-100 text-gray-600',
  EXPORTAR:            'bg-purple-100 text-purple-700',
  LOGIN:               'bg-teal-100 text-teal-700',
  LOGOUT:              'bg-gray-100 text-gray-500',
  EJECUTAR_SIMULACION: 'bg-orange-100 text-orange-700',
};

const ACCIONES = ['CREAR', 'ACTUALIZAR', 'ELIMINAR', 'LEER', 'EXPORTAR', 'LOGIN', 'LOGOUT', 'EJECUTAR_SIMULACION'];

export function Bitacora() {
  const [eventos, setEventos] = useState<BitacoraEventoDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [filtroAccion, setFiltroAccion] = useState<string | null>(null);
  const [desde, setDesde] = useState('');
  const [hasta, setHasta] = useState('');
  const [page, setPage] = useState(1);

  const load = () => {
    setLoading(true);
    bitacoraApi.getAll({
      accion: filtroAccion ?? undefined,
      desde: desde || undefined,
      hasta: hasta || undefined,
      page,
      pageSize: 50,
    })
      .then(r => { setEventos(r.items); setTotalCount(r.totalCount); })
      .catch(() => toast.error('Error al cargar la bitácora'))
      .finally(() => setLoading(false));
  };

  useEffect(load, [filtroAccion, desde, hasta, page]);

  const totalPages = Math.ceil(totalCount / 50);

  const filtered = eventos.filter(e =>
    !search ||
    e.usuarioEmail?.toLowerCase().includes(search.toLowerCase()) ||
    e.recurso?.toLowerCase().includes(search.toLowerCase()) ||
    e.accion.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Bitácora de Auditoría</h1>
          <p className="text-sm text-gray-500 mt-0.5">Registro inmutable de todas las acciones del sistema</p>
        </div>
        <button onClick={load} className="flex items-center gap-2 text-sm text-gray-500 hover:text-gray-800 transition">
          <RefreshCw size={15} className={loading ? 'animate-spin' : ''} />
          Actualizar
        </button>
      </div>

      {/* Filtros */}
      <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4 mb-4 space-y-3">
        <div className="flex gap-3">
          <div className="relative flex-1">
            <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input type="text" placeholder="Buscar por usuario, recurso o acción..." value={search}
              onChange={e => setSearch(e.target.value)}
              className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="flex items-center gap-2">
            <label className="text-xs text-gray-500">Desde</label>
            <input type="date" value={desde} onChange={e => { setDesde(e.target.value); setPage(1); }}
              className="border border-gray-300 rounded-lg px-3 py-2 text-xs focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="flex items-center gap-2">
            <label className="text-xs text-gray-500">Hasta</label>
            <input type="date" value={hasta} onChange={e => { setHasta(e.target.value); setPage(1); }}
              className="border border-gray-300 rounded-lg px-3 py-2 text-xs focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <Filter size={13} className="text-gray-400" />
          <button onClick={() => { setFiltroAccion(null); setPage(1); }}
            className={`px-2.5 py-1 rounded-lg text-xs font-medium transition ${
              !filtroAccion ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}>Todas</button>
          {ACCIONES.map(a => (
            <button key={a} onClick={() => { setFiltroAccion(f => f === a ? null : a); setPage(1); }}
              className={`px-2.5 py-1 rounded-lg text-xs font-medium transition ${
                filtroAccion === a ? 'bg-blue-600 text-white' :
                `${ACCION_COLORS[a] ?? 'bg-gray-100 text-gray-600'} hover:opacity-80`
              }`}>{a}</button>
          ))}
        </div>
      </div>

      <div className="flex items-center justify-between mb-3">
        <p className="text-xs text-gray-500">
          Mostrando {filtered.length} de {totalCount} eventos
          {filtroAccion && ` · filtrado por ${filtroAccion}`}
        </p>
        {totalPages > 1 && (
          <div className="flex items-center gap-2">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
              className="px-2.5 py-1 rounded text-xs bg-gray-100 disabled:opacity-40 hover:bg-gray-200 transition">← Ant.</button>
            <span className="text-xs text-gray-500">{page} / {totalPages}</span>
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}
              className="px-2.5 py-1 rounded text-xs bg-gray-100 disabled:opacity-40 hover:bg-gray-200 transition">Sig. →</button>
          </div>
        )}
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Cargando bitácora...</div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border border-gray-100">
          <BookOpen size={40} className="mx-auto text-gray-300 mb-3" />
          <p className="text-gray-500">No hay eventos en el período seleccionado</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-100">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Fecha / Hora</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Usuario</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Acción</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Recurso</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Descripción</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Resultado</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">IP</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {filtered.map(e => <BitacoraRow key={e.id} evento={e} />)}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function BitacoraRow({ evento }: { evento: BitacoraEventoDto }) {
  const [expanded, setExpanded] = useState(false);
  const colorClass = ACCION_COLORS[evento.accion] ?? 'bg-gray-100 text-gray-600';
  return (
    <>
      <tr className="hover:bg-gray-50 transition-colors cursor-pointer" onClick={() => setExpanded(e => !e)}>
        <td className="px-4 py-3 text-xs text-gray-500 whitespace-nowrap">
          {new Date(evento.ocurridoAt).toLocaleString('es-CR')}
        </td>
        <td className="px-4 py-3">
          <p className="text-xs font-medium text-gray-700 truncate max-w-[160px]">
            {evento.usuarioEmail ?? evento.usuarioId}
          </p>
        </td>
        <td className="px-4 py-3">
          <span className={`text-[11px] font-semibold px-2 py-0.5 rounded ${colorClass}`}>{evento.accion}</span>
        </td>
        <td className="px-4 py-3 text-xs text-gray-600 truncate max-w-[120px]">
          {evento.recurso ?? '—'}
          {evento.recursoId && (
            <span className="ml-1 text-gray-400 font-mono text-[10px]">#{evento.recursoId.slice(0, 8)}</span>
          )}
        </td>
        <td className="px-4 py-3 text-xs text-gray-500 truncate max-w-[200px]">{evento.descripcion ?? '—'}</td>
        <td className="px-4 py-3">
          {evento.exitoso
            ? <CheckCircle2 size={14} className="text-green-500" />
            : <XCircle size={14} className="text-red-500" />}
        </td>
        <td className="px-4 py-3 text-xs text-gray-400 font-mono">{evento.ipOrigen ?? '—'}</td>
      </tr>
      {expanded && (evento.datosAntes || evento.datosDespues) && (
        <tr className="bg-blue-50">
          <td colSpan={7} className="px-4 py-3">
            <div className="grid grid-cols-2 gap-4 text-xs">
              {evento.datosAntes && (
                <div>
                  <p className="font-semibold text-gray-500 mb-1">Antes:</p>
                  <pre className="bg-white border border-gray-200 rounded p-2 overflow-auto max-h-32 text-gray-700">{evento.datosAntes}</pre>
                </div>
              )}
              {evento.datosDespues && (
                <div>
                  <p className="font-semibold text-gray-500 mb-1">Después:</p>
                  <pre className="bg-white border border-gray-200 rounded p-2 overflow-auto max-h-32 text-gray-700">{evento.datosDespues}</pre>
                </div>
              )}
            </div>
          </td>
        </tr>
      )}
    </>
  );
}
