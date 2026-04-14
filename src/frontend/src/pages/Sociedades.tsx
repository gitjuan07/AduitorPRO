import { useState, useEffect } from 'react';
import { sociedadesApi, type SociedadDto } from '../api/sociedades';
import { toast } from 'sonner';
import { Building2, Search, RefreshCw, CheckCircle2, XCircle } from 'lucide-react';

const PAIS_FLAG: Record<string, string> = {
  'Costa Rica':           '🇨🇷',
  'República Dominicana': '🇩🇴',
  'Guatemala':            '🇬🇹',
  'Honduras':             '🇭🇳',
  'Nicaragua':            '🇳🇮',
  'Panamá':               '🇵🇦',
  'El Salvador':          '🇸🇻',
};

export function Sociedades() {
  const [sociedades, setSociedades] = useState<SociedadDto[]>([]);
  const [loading, setLoading]       = useState(true);
  const [search, setSearch]         = useState('');
  const [soloActivas, setSoloActivas] = useState(false);

  const load = () => {
    setLoading(true);
    sociedadesApi.getAll(soloActivas || undefined)
      .then(setSociedades)
      .catch(() => toast.error('Error al cargar las sociedades'))
      .finally(() => setLoading(false));
  };

  useEffect(load, [soloActivas]);

  const filtered = sociedades.filter(s =>
    !search ||
    s.codigo.toLowerCase().includes(search.toLowerCase()) ||
    s.nombre.toLowerCase().includes(search.toLowerCase()) ||
    s.pais?.toLowerCase().includes(search.toLowerCase())
  );

  // Agrupar por país para el resumen
  const porPais = filtered.reduce<Record<string, number>>((acc, s) => {
    const p = s.pais ?? 'Sin país';
    acc[p] = (acc[p] ?? 0) + 1;
    return acc;
  }, {});

  return (
    <div className="p-6">
      {/* Encabezado */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Catálogo de Sociedades</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            {filtered.length} sociedad{filtered.length !== 1 ? 'es' : ''} — ILG Logistics Group
          </p>
        </div>
        <button
          onClick={load}
          disabled={loading}
          className="flex items-center gap-2 px-3 py-2 text-sm bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
        >
          <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
          Actualizar
        </button>
      </div>

      {/* Resumen por país */}
      <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-7 gap-3 mb-6">
        {Object.entries(porPais).map(([pais, count]) => (
          <div key={pais} className="bg-white rounded-xl border border-gray-200 px-3 py-2.5 text-center">
            <div className="text-xl">{PAIS_FLAG[pais] ?? '🏢'}</div>
            <div className="text-lg font-bold text-gray-900">{count}</div>
            <div className="text-[10px] text-gray-500 leading-tight">{pais}</div>
          </div>
        ))}
      </div>

      {/* Filtros */}
      <div className="flex gap-3 mb-4">
        <div className="relative flex-1 max-w-sm">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            placeholder="Buscar por código, nombre o país…"
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="w-full pl-8 pr-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
          />
        </div>
        <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer select-none">
          <input
            type="checkbox"
            checked={soloActivas}
            onChange={e => setSoloActivas(e.target.checked)}
            className="rounded text-red-600 focus:ring-red-500"
          />
          Solo activas
        </label>
      </div>

      {/* Tabla */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden shadow-sm">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 font-semibold text-gray-600 w-24">Código</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Nombre</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">País</th>
              <th className="text-center px-4 py-3 font-semibold text-gray-600 w-24">Estado</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={4} className="text-center py-12 text-gray-400">
                  <RefreshCw size={20} className="animate-spin mx-auto mb-2" />
                  Cargando…
                </td>
              </tr>
            ) : filtered.length === 0 ? (
              <tr>
                <td colSpan={4} className="text-center py-12 text-gray-400">
                  <Building2 size={32} className="mx-auto mb-2 opacity-30" />
                  No se encontraron sociedades
                </td>
              </tr>
            ) : (
              filtered.map(s => (
                <tr key={s.id} className="border-b border-gray-100 hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3">
                    <span className="font-mono text-xs bg-gray-100 text-gray-700 px-2 py-0.5 rounded font-medium">
                      {s.codigo}
                    </span>
                  </td>
                  <td className="px-4 py-3 font-medium text-gray-900">{s.nombre}</td>
                  <td className="px-4 py-3 text-gray-600">
                    {s.pais && (
                      <span className="flex items-center gap-1.5">
                        <span>{PAIS_FLAG[s.pais] ?? '🏢'}</span>
                        {s.pais}
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-center">
                    {s.activa ? (
                      <span className="inline-flex items-center gap-1 text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-medium">
                        <CheckCircle2 size={11} /> Activa
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 text-xs bg-gray-100 text-gray-500 px-2 py-0.5 rounded-full font-medium">
                        <XCircle size={11} /> Inactiva
                      </span>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
