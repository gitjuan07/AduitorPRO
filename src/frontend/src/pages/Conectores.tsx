import { useState, useEffect, useRef } from 'react';
import { conectoresApi, type ConectorDto, type EjecutarResult } from '../api/conectores';
import { toast } from 'sonner';
import {
  Plug, Plus, Play, Settings, Trash2, CheckCircle2, XCircle,
  AlertCircle, HelpCircle, RefreshCw, Clock, Database, Table2, FlaskConical
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
  BASE_DATOS: 'Base de datos SQL',
  EXCEL_CSV:  'Excel / CSV',
  SAP_RFC:    'SAP RFC',
  WEBHOOK:    'Webhook',
};

interface SqlConfig { servidor: string; baseDatos: string; usuario: string; passwordPlain: string; vista: string; queryPersonalizado: string; }
const EMPTY_SQL: SqlConfig = { servidor: '', baseDatos: '', usuario: '', passwordPlain: '', vista: '', queryPersonalizado: '' };
const EMPTY_FORM: Partial<ConectorDto> = { nombre: '', sistema: '', tipoConexion: 'REST_API', descripcion: '', activo: true };

export function Conectores() {
  const formRef = useRef<HTMLDivElement>(null);
  const [conectores, setConectores] = useState<ConectorDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<Partial<ConectorDto>>(EMPTY_FORM);
  const [sqlConfig, setSqlConfig] = useState<SqlConfig>(EMPTY_SQL);
  const [testingId, setTestingId] = useState<string | null>(null);
  const [ejecutandoId, setEjecutandoId] = useState<string | null>(null);
  const [resultados, setResultados] = useState<EjecutarResult | null>(null);
  const [saving, setSaving] = useState(false);
  const [probandoQuery, setProbandoQuery] = useState(false);
  const [resultadoQuery, setResultadoQuery] = useState<EjecutarResult | null>(null);

  const load = () => {
    setLoading(true);
    conectoresApi.getAll()
      .then(setConectores)
      .catch(() => toast.error('Error al cargar conectores'))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const esBaseDatos = (form.tipoConexion ?? '') === 'BASE_DATOS';

  const handleSubmit = async () => {
    if (!form.nombre || !form.sistema) { toast.error('Nombre y sistema son obligatorios'); return; }
    setSaving(true);
    try {
      // Para BASE_DATOS: serializar la config SQL en ConfiguracionJson
      const payload = esBaseDatos
        ? { ...form, configuracionJson: JSON.stringify({ ...sqlConfig, passwordPlain: sqlConfig.passwordPlain }) }
        : form;

      if (editingId) {
        await conectoresApi.actualizar(editingId, payload);
        toast.success('Conector actualizado');
      } else {
        await conectoresApi.crear(payload);
        toast.success('Conector creado');
      }
      setShowForm(false); setEditingId(null); setForm(EMPTY_FORM); setSqlConfig(EMPTY_SQL); setResultadoQuery(null);
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
      if (res.exitoso) toast.success(`Conexión exitosa — ${res.duracionMs}ms`);
      else toast.error(`Fallo: ${res.mensaje}`);
      load();
    } catch {
      toast.error('Error al probar la conexión');
    } finally {
      setTestingId(null);
    }
  };

  const handleEjecutar = async (id: string) => {
    setEjecutandoId(id);
    setResultados(null);
    try {
      const res = await conectoresApi.ejecutar(id);
      setResultados(res);
      if (res.exitoso) toast.success(`${res.totalFilas} registros obtenidos en ${res.duracionMs}ms`);
      else toast.error(`Error: ${res.mensaje}`);
    } catch {
      toast.error('Error al ejecutar el conector');
    } finally {
      setEjecutandoId(null);
    }
  };

  const handleProbarQuery = async () => {
    if (!editingId) return;
    setProbandoQuery(true);
    setResultadoQuery(null);
    try {
      const res = await conectoresApi.probarQuery(editingId, JSON.stringify(sqlConfig));
      setResultadoQuery(res);
      if (res.exitoso) toast.success(`Query OK — ${res.totalFilas} filas en ${res.duracionMs}ms`);
      else toast.error(`Error: ${res.mensaje}`);
    } catch {
      toast.error('Error al probar el query');
    } finally {
      setProbandoQuery(false);
    }
  };

  const handleDelete = async (c: ConectorDto) => {
    if (!confirm(`¿Eliminar el conector "${c.nombre}"?`)) return;
    try {
      await conectoresApi.eliminar(c.id);
      toast.success('Conector eliminado');
      setConectores(prev => prev.filter(x => x.id !== c.id));
      if (resultados) setResultados(null);
    } catch {
      toast.error('Error al eliminar');
    }
  };

  const startEdit = (c: ConectorDto) => {
    setForm(c);
    setEditingId(c.id);
    if (c.tipoConexion === 'BASE_DATOS' && c.configuracionJson) {
      try { setSqlConfig({ ...EMPTY_SQL, ...JSON.parse(c.configuracionJson) }); }
      catch { setSqlConfig(EMPTY_SQL); }
    } else {
      setSqlConfig(EMPTY_SQL);
    }
    setShowForm(true);
    // Scroll al formulario
    setTimeout(() => formRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 50);
  };

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Conectores</h1>
          <p className="text-sm text-gray-500 mt-0.5">SOA Manager — Integración con sistemas externos</p>
        </div>
        <button onClick={() => { setForm(EMPTY_FORM); setSqlConfig(EMPTY_SQL); setEditingId(null); setShowForm(true); }}
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition text-sm font-medium">
          <Plus size={16} /> Nuevo conector
        </button>
      </div>

      {/* Formulario */}
      {showForm && (
        <div ref={formRef} className="mb-6 bg-white rounded-xl border border-blue-200 shadow-sm p-6">
          <h2 className="font-semibold text-gray-800 mb-4 flex items-center gap-2">
            {esBaseDatos ? <Database size={16} className="text-blue-600" /> : <Plug size={16} className="text-blue-600" />}
            {editingId ? 'Editar conector' : 'Nuevo conector'}
          </h2>

          {/* Campos base */}
          <div className="grid grid-cols-2 gap-4 mb-4">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Nombre *</label>
              <input type="text" value={form.nombre ?? ''} onChange={e => setForm(f => ({ ...f, nombre: e.target.value }))}
                placeholder="SE Suite — Vista Usuarios" className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Sistema destino *</label>
              <input type="text" value={form.sistema ?? ''} onChange={e => setForm(f => ({ ...f, sistema: e.target.value }))}
                placeholder="SE_SUITE, SAP, EVOLUTION..." className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
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
                placeholder="Descripción opcional" className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>

          {/* Campos SQL — solo para BASE_DATOS */}
          {esBaseDatos && (
            <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 mb-4">
              <p className="text-xs font-semibold text-blue-700 mb-3 flex items-center gap-1.5">
                <Database size={13} /> Configuración de base de datos SQL Server
              </p>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Servidor *</label>
                  <input type="text" value={sqlConfig.servidor} onChange={e => setSqlConfig(s => ({ ...s, servidor: e.target.value }))}
                    placeholder="servidor.database.windows.net" className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Base de datos *</label>
                  <input type="text" value={sqlConfig.baseDatos} onChange={e => setSqlConfig(s => ({ ...s, baseDatos: e.target.value }))}
                    placeholder="NombreBaseDatos" className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Usuario de servicio *</label>
                  <input type="text" value={sqlConfig.usuario} onChange={e => setSqlConfig(s => ({ ...s, usuario: e.target.value }))}
                    placeholder="sa_auditoria" className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Contraseña</label>
                  <input type="password" value={sqlConfig.passwordPlain} onChange={e => setSqlConfig(s => ({ ...s, passwordPlain: e.target.value }))}
                    placeholder="••••••••" className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white" />
                  <p className="text-[11px] text-blue-600 mt-1">Se guardará encriptada en Key Vault en versión siguiente</p>
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Vista o tabla</label>
                  <input type="text" value={sqlConfig.vista} onChange={e => setSqlConfig(s => ({ ...s, vista: e.target.value }))}
                    placeholder="dbo.v_usuarios_activos" className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white" />
                </div>
                <div className="col-span-2">
                  <label className="block text-xs font-medium text-gray-600 mb-1">Query personalizado (opcional)</label>
                  <div className="flex gap-2">
                    <input type="text" value={sqlConfig.queryPersonalizado} onChange={e => setSqlConfig(s => ({ ...s, queryPersonalizado: e.target.value }))}
                      placeholder="SELECT TOP 100 * FROM dbo.v_roles WHERE activo=1" className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white" />
                    {editingId && (
                      <button type="button" onClick={handleProbarQuery} disabled={probandoQuery}
                        className="flex items-center gap-1.5 px-3 py-2 bg-indigo-600 text-white rounded-lg text-xs font-medium hover:bg-indigo-700 disabled:opacity-50 transition whitespace-nowrap">
                        {probandoQuery ? <RefreshCw size={13} className="animate-spin" /> : <FlaskConical size={13} />}
                        {probandoQuery ? 'Probando...' : 'Probar query'}
                      </button>
                    )}
                  </div>
                  <p className="text-[11px] text-gray-400 mt-1">
                    Si se define, tiene prioridad sobre la vista.
                    {!editingId && <span className="text-indigo-500 ml-1">Guarda el conector primero para poder probar el query.</span>}
                  </p>
                </div>
              </div>

              {/* Resultado inline del probar-query */}
              {resultadoQuery && (
                <div className={`mt-3 rounded-lg border text-xs overflow-hidden ${resultadoQuery.exitoso ? 'border-green-200 bg-green-50' : 'border-red-200 bg-red-50'}`}>
                  <div className={`px-3 py-2 flex items-center justify-between border-b ${resultadoQuery.exitoso ? 'border-green-200' : 'border-red-200'}`}>
                    <div className="flex items-center gap-1.5">
                      {resultadoQuery.exitoso
                        ? <CheckCircle2 size={13} className="text-green-600" />
                        : <XCircle size={13} className="text-red-600" />}
                      <span className="font-semibold text-gray-700">
                        {resultadoQuery.exitoso
                          ? `${resultadoQuery.totalFilas} filas · ${resultadoQuery.duracionMs}ms`
                          : `Error: ${resultadoQuery.mensaje}`}
                      </span>
                    </div>
                    <button onClick={() => setResultadoQuery(null)} className="text-gray-400 hover:text-gray-600">✕</button>
                  </div>
                  {resultadoQuery.exitoso && resultadoQuery.columnas.length > 0 && (
                    <div className="overflow-auto max-h-48">
                      <table className="w-full">
                        <thead className="bg-white/60 sticky top-0">
                          <tr>{resultadoQuery.columnas.map(col => (
                            <th key={col} className="text-left px-2 py-1.5 font-semibold text-gray-600 border-b border-green-100 whitespace-nowrap">{col}</th>
                          ))}</tr>
                        </thead>
                        <tbody className="divide-y divide-green-50">
                          {resultadoQuery.filas.map((fila, i) => (
                            <tr key={i} className="hover:bg-white/40">
                              {fila.map((cel, j) => (
                                <td key={j} className="px-2 py-1 text-gray-700 whitespace-nowrap max-w-[160px] truncate">
                                  {cel === null ? <span className="text-gray-300 italic">null</span> : String(cel)}
                                </td>
                              ))}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              )}
            </div>
          )}

          <div className="flex items-center gap-3">
            <button onClick={handleSubmit} disabled={saving}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition">
              {saving ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear conector'}
            </button>
            <button onClick={() => { setShowForm(false); setEditingId(null); setForm(EMPTY_FORM); setSqlConfig(EMPTY_SQL); setResultadoQuery(null); }}
              className="px-4 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-100 transition">Cancelar</button>
          </div>
        </div>
      )}

      {/* Tabla de conectores */}
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
                      <div className="flex items-center gap-1"><Clock size={11} />{new Date(c.ultimoTest).toLocaleString('es-CR')}</div>
                    ) : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1 justify-end">
                      {/* Probar */}
                      <button onClick={() => handleTest(c.id)} disabled={testingId === c.id}
                        className="p-1.5 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded transition" title="Probar conexión">
                        {testingId === c.id ? <RefreshCw size={14} className="animate-spin" /> : <Play size={14} />}
                      </button>
                      {/* Ejecutar (solo BD) */}
                      {c.tipoConexion === 'BASE_DATOS' && (
                        <button onClick={() => handleEjecutar(c.id)} disabled={ejecutandoId === c.id}
                          className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded transition" title="Ejecutar y ver resultados">
                          {ejecutandoId === c.id ? <RefreshCw size={14} className="animate-spin" /> : <Table2 size={14} />}
                        </button>
                      )}
                      <button onClick={() => startEdit(c)}
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

      {/* Resultados de ejecución */}
      {resultados && (
        <div className="mt-6 bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className={`px-5 py-3 border-b flex items-center justify-between ${resultados.exitoso ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'}`}>
            <div className="flex items-center gap-2">
              {resultados.exitoso
                ? <CheckCircle2 size={16} className="text-green-600" />
                : <XCircle size={16} className="text-red-600" />}
              <span className="text-sm font-semibold text-gray-800">
                {resultados.exitoso ? `${resultados.totalFilas} registros · ${resultados.duracionMs}ms` : `Error: ${resultados.mensaje}`}
              </span>
            </div>
            <button onClick={() => setResultados(null)} className="text-gray-400 hover:text-gray-600 text-xs">✕ Cerrar</button>
          </div>
          {resultados.exitoso && resultados.columnas.length > 0 && (
            <div className="overflow-auto max-h-96">
              <table className="w-full text-xs">
                <thead className="bg-gray-50 sticky top-0">
                  <tr>
                    {resultados.columnas.map(col => (
                      <th key={col} className="text-left px-3 py-2 font-semibold text-gray-600 border-b border-gray-200 whitespace-nowrap">{col}</th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {resultados.filas.map((fila, i) => (
                    <tr key={i} className="hover:bg-gray-50">
                      {fila.map((cel, j) => (
                        <td key={j} className="px-3 py-1.5 text-gray-700 whitespace-nowrap max-w-xs truncate">
                          {cel === null ? <span className="text-gray-300 italic">null</span> : String(cel)}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
