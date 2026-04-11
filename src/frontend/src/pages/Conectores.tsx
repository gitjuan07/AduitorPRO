import { useState, useEffect } from 'react';
import { conectoresApi, type ConectorDto } from '../api/conectores';
import { toast } from 'sonner';
import {
  Plug, Plus, Play, Settings, Trash2, CheckCircle2, XCircle,
  AlertCircle, HelpCircle, RefreshCw, Clock
} from 'lucide-react';

const ESTADO_ICON: Record<string, React.ReactNode> = {
  VERDE:         <CheckCircle2 size={14} className="text-green-500" />,
  AMARILLO:      <AlertCircle  size={14} className="text-yellow-500" />,
  ROJO:          <XCircle      size={14} className="text-red-500" />,
  DESCONOCIDO:   <HelpCircle   size={14} className="text-gray-400" />,
  ACTIVO:        <CheckCircle2 size={14} className="text-green-500" />,
  ERROR:         <XCircle      size={14} className="text-red-500" />,
  INACTIVO:      <HelpCircle   size={14} className="text-gray-400" />,
  MANTENIMIENTO: <AlertCircle  size={14} className="text-yellow-500" />,
};

const TIPO_LABELS: Record<string, string> = {
  REST_API:   'REST API',
  SFTP:       'SFTP',
  BASE_DATOS: 'Base de datos',
  EXCEL_CSV:  'Excel / CSV',
  SAP_RFC:    'SAP RFC',
  WEBHOOK:    'Webhook',
};

const EMPTY_FORM: Partial<ConectorDto> = { nombre: '', sistema: '', tipoConexion: 'REST_API', descripcion: '', activo: true };

export function Conectores() {
  const [conectores, setConectores] = useState<ConectorDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<Partial<ConectorDto>>(EMPTY_FORM);
  const [testingId, setTestingId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const load = () => {
    setLoading(true);
    conectoresApi.getAll()
      .then(setConectores)
      .catch(() => toast.error('Error al cargar conectores'))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const handleSubmit = async () => {
    if (!form.nombre || !form.sistema) { toast.error('Nombre y sistema son obligatorios'); return; }
    setSaving(true);
    try {
      if (editingId) {
        await conectoresApi.actualizar(editingId, form);
        toast.success('Conector actualizado');
      } else {
        await conectoresApi.crear(form);
        toast.success('Conector creado');
      }
      setShowForm(false); setEditingId(null); setForm(EMPTY_FORM);
      load();
    } catch {
      toast.error('Error al guardar el conector');
    } finally {
      setSaving(false);
    }
  };

  const handleTest = async (id: string) => {
    setTestingId(id);
    try {
      const res = await conectoresApi.probar(id);
      if (res.exitoso) toast.success(`✅ Conexión exitosa en ${res.duracionMs}ms`);
      else toast.error(`❌ Fallo: ${res.mensaje}`);
      load();
    } catch {
      toast.error('Error al probar la conexión');
    } finally {
      setTestingId(null);
    }
  };

  const handleDelete = async (c: ConectorDto) => {
    if (!confirm(`¿Eliminar el conector "${c.nombre}"?`)) return;
    try {
      await conectoresApi.eliminar(c.id);
      toast.success('Conector eliminado');
      setConectores(prev => prev.filter(x => x.id !== c.id));
    } catch {
      toast.error('Error al eliminar');
    }
  };

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Conectores</h1>
          <p className="text-sm text-gray-500 mt-0.5">SOA Manager — Integración con sistemas externos</p>
        </div>
        <button onClick={() => { setForm(EMPTY_FORM); setEditingId(null); setShowForm(true); }}
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition text-sm font-medium">
          <Plus size={16} /> Nuevo conector
        </button>
      </div>

      {/* Formulario */}
      {showForm && (
        <div className="mb-6 bg-white rounded-xl border border-blue-200 shadow-sm p-6">
          <h2 className="font-semibold text-gray-800 mb-4">{editingId ? 'Editar conector' : 'Nuevo conector'}</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Nombre *</label>
              <input type="text" value={form.nombre ?? ''} onChange={e => setForm(f => ({ ...f, nombre: e.target.value }))}
                placeholder="SE Suite — Producción"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Sistema destino *</label>
              <input type="text" value={form.sistema ?? ''} onChange={e => setForm(f => ({ ...f, sistema: e.target.value }))}
                placeholder="SE_SUITE, SAP, EVOLUTION..."
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Tipo de conexión</label>
              <select value={form.tipoConexion ?? 'REST_API'} onChange={e => setForm(f => ({ ...f, tipoConexion: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                {Object.entries(TIPO_LABELS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Descripción</label>
              <input type="text" value={form.descripcion ?? ''} onChange={e => setForm(f => ({ ...f, descripcion: e.target.value }))}
                placeholder="Descripción opcional"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          <div className="flex items-center gap-3 mt-4">
            <button onClick={handleSubmit} disabled={saving}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition">
              {saving ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear conector'}
            </button>
            <button onClick={() => { setShowForm(false); setEditingId(null); setForm(EMPTY_FORM); }}
              className="px-4 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-100 transition">Cancelar</button>
          </div>
        </div>
      )}

      {loading ? (
        <div className="text-center py-12 text-gray-400">Cargando conectores...</div>
      ) : conectores.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border border-gray-100">
          <Plug size={40} className="mx-auto text-gray-300 mb-3" />
          <p className="text-gray-500 font-medium">No hay conectores configurados</p>
          <p className="text-sm text-gray-400 mt-1">Crea el primer conector para comenzar las integraciones</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-100">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Conector</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Sistema</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Tipo</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Estado</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Último test</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {conectores.map(c => (
                <tr key={c.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3">
                    <p className="font-medium text-gray-800">{c.nombre}</p>
                    {c.descripcion && <p className="text-xs text-gray-400 mt-0.5 truncate max-w-xs">{c.descripcion}</p>}
                  </td>
                  <td className="px-4 py-3 text-gray-600">{c.sistema}</td>
                  <td className="px-4 py-3">
                    <span className="px-2 py-0.5 bg-gray-100 text-gray-600 rounded text-xs font-medium">
                      {TIPO_LABELS[c.tipoConexion] ?? c.tipoConexion}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1.5">
                      {ESTADO_ICON[c.estado] ?? ESTADO_ICON.DESCONOCIDO}
                      <span className="text-xs text-gray-600">{c.estado}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-xs text-gray-400">
                    {c.ultimoTest ? (
                      <div className="flex items-center gap-1">
                        <Clock size={11} />
                        {new Date(c.ultimoTest).toLocaleString('es-CR')}
                      </div>
                    ) : '—'}
                    {c.ultimoTestResultado && <p className="text-[11px] truncate max-w-[180px] mt-0.5">{c.ultimoTestResultado}</p>}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1 justify-end">
                      <button onClick={() => handleTest(c.id)} disabled={testingId === c.id}
                        className="p-1.5 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded transition" title="Probar conexión">
                        {testingId === c.id ? <RefreshCw size={14} className="animate-spin" /> : <Play size={14} />}
                      </button>
                      <button onClick={() => { setForm(c); setEditingId(c.id); setShowForm(true); }}
                        className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded transition" title="Editar">
                        <Settings size={14} />
                      </button>
                      <button onClick={() => handleDelete(c)}
                        className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition" title="Eliminar">
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
