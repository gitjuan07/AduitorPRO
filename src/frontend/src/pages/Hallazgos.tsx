import { useEffect, useRef, useState } from 'react';
import { getHallazgos, actualizarPlanAccion, cerrarHallazgo, type HallazgoDto } from '../api/hallazgos';
import { evidenciasApi, type EvidenciaDto } from '../api/evidencias';
import { Semaforo } from '../components/ui/Semaforo';
import { toast } from 'sonner';
import {
  AlertTriangle, ChevronDown, ChevronUp, FileText, Upload,
  X, CheckCircle, Paperclip, RefreshCw, Filter, Download
} from 'lucide-react';

// ─── tipos ────────────────────────────────────────────────────────────────────

const CRITICIDAD_CONFIG: Record<string, { label: string; cls: string; dot: string }> = {
  CRITICA: { label: 'CRÍTICA', cls: 'bg-red-100 text-red-800 border border-red-200',   dot: 'bg-red-500' },
  MEDIA:   { label: 'MEDIA',   cls: 'bg-yellow-100 text-yellow-800 border border-yellow-200', dot: 'bg-yellow-500' },
  BAJA:    { label: 'BAJA',    cls: 'bg-blue-100 text-blue-800 border border-blue-200',   dot: 'bg-blue-400' },
};

const ESTADO_SEMAFORO: Record<string, 'VERDE' | 'AMARILLO' | 'ROJO' | 'NO_EVALUADO'> = {
  ABIERTO:    'ROJO',
  EN_PROCESO: 'AMARILLO',
  RESUELTO:   'VERDE',
  CERRADO:    'VERDE',
  ACEPTADO:   'AMARILLO',
};

const TIPOS_HALLAZGO: Record<string, string> = {
  ACCESO_EX_EMPLEADO:              'Ex-empleado con acceso SAP',
  CONFLICTO_SOD:                   'Conflicto SoD',
  ROL_NO_AUTORIZADO_MATRIZ:        'Rol no autorizado en Matriz',
  CASO_SESUITE_VENCIDO:            'Caso SE Suite vencido',
  EMPLEADO_SIN_CUENTA_SAP:         'Empleado sin cuenta SAP',
  SAP_ACTIVO_ENTRA_ID_DESHABILITADO: 'SAP activo / Entra ID deshabilitado',
  EX_EMPLEADO_ENTRA_ID_ACTIVO:     'Ex-empleado con Entra ID activo',
  EMPLEADO_SIN_CUENTA_ENTRA_ID:    'Empleado sin Entra ID',
};

// ─── componente principal ─────────────────────────────────────────────────────

export function Hallazgos() {
  const [items, setItems]         = useState<HallazgoDto[]>([]);
  const [loading, setLoading]     = useState(true);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  // Filtros
  const [filtroCrit, setFiltroCrit]   = useState('');
  const [filtroEst, setFiltroEst]     = useState('');
  const [filtroTipo, setFiltroTipo]   = useState('');
  const [filtroBusq, setFiltroBusq]   = useState('');

  // Modal plan
  const [planModal, setPlanModal]         = useState<HallazgoDto | null>(null);
  const [planText, setPlanText]           = useState('');
  const [planResponsable, setPlanResponsable] = useState('');
  const [planFecha, setPlanFecha]         = useState('');
  const [savingPlan, setSavingPlan]       = useState(false);

  // Modal evidencias
  const [evidModal, setEvidModal]         = useState<HallazgoDto | null>(null);
  const [evidencias, setEvidencias]       = useState<EvidenciaDto[]>([]);
  const [loadingEvid, setLoadingEvid]     = useState(false);
  const [uploadingEvid, setUploadingEvid] = useState(false);
  const [descEvid, setDescEvid]           = useState('');
  const fileRef = useRef<HTMLInputElement>(null);

  const load = () => {
    setLoading(true);
    getHallazgos({ pageSize: 200 })
      .then(r => setItems(r.items))
      .catch(() => toast.error('Error al cargar hallazgos'))
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  // ─── filtrado local ──────────────────────────────────────────────────────
  const filtered = items.filter(h => {
    if (filtroCrit && h.criticidad !== filtroCrit) return false;
    if (filtroEst  && h.estado !== filtroEst)      return false;
    if (filtroTipo && h.tipoHallazgo !== filtroTipo) return false;
    if (filtroBusq) {
      const q = filtroBusq.toLowerCase();
      return h.titulo.toLowerCase().includes(q)
          || (h.usuarioSAP ?? '').toLowerCase().includes(q)
          || (h.cedula ?? '').includes(q)
          || (h.rolAfectado ?? '').toLowerCase().includes(q);
    }
    return true;
  });

  // Stats
  const criticos   = items.filter(h => h.criticidad === 'CRITICA').length;
  const abiertos   = items.filter(h => h.estado === 'ABIERTO').length;
  const enProceso  = items.filter(h => h.estado === 'EN_PROCESO').length;

  // ─── plan de acción ──────────────────────────────────────────────────────
  const abrirPlanModal = (h: HallazgoDto) => {
    setPlanModal(h);
    setPlanText(h.planAccion ?? '');
    setPlanResponsable(h.responsableEmail ?? '');
    setPlanFecha(h.fechaCompromiso ?? '');
  };

  const guardarPlan = async () => {
    if (!planModal) return;
    setSavingPlan(true);
    try {
      await actualizarPlanAccion(planModal.id, {
        planAccion: planText,
        responsableEmail: planResponsable || undefined,
        fechaCompromiso: planFecha || undefined,
      });
      toast.success('Plan de acción guardado');
      setPlanModal(null);
      load();
    } catch {
      toast.error('Error al guardar plan');
    } finally { setSavingPlan(false); }
  };

  const cerrar = async (h: HallazgoDto) => {
    if (!confirm(`¿Cerrar el hallazgo "${h.titulo}"?`)) return;
    try {
      await cerrarHallazgo(h.id, 'Cerrado desde interfaz de auditoría');
      toast.success('Hallazgo cerrado');
      load();
    } catch { toast.error('Error al cerrar'); }
  };

  // ─── evidencias ──────────────────────────────────────────────────────────
  const abrirEvidencias = async (h: HallazgoDto) => {
    setEvidModal(h);
    setEvidencias([]);
    setLoadingEvid(true);
    try {
      const r = await evidenciasApi.getAll({ hallazgoId: h.id, pageSize: 50 });
      setEvidencias(r.items);
    } catch { toast.error('Error al cargar evidencias'); }
    finally { setLoadingEvid(false); }
  };

  const subirEvidencia = async (file: File) => {
    if (!evidModal) return;
    setUploadingEvid(true);
    try {
      const fd = new FormData();
      fd.append('archivo', file);
      fd.append('hallazgoId', evidModal.id);
      fd.append('tipoEvidencia', 'DOCUMENTO');
      if (descEvid) fd.append('descripcionArchivo', descEvid);
      await evidenciasApi.upload(fd);
      toast.success('Evidencia subida');
      setDescEvid('');
      const r = await evidenciasApi.getAll({ hallazgoId: evidModal.id, pageSize: 50 });
      setEvidencias(r.items);
    } catch { toast.error('Error al subir evidencia'); }
    finally { setUploadingEvid(false); }
  };

  const eliminarEvidencia = async (id: string) => {
    if (!confirm('¿Eliminar esta evidencia?')) return;
    try {
      await evidenciasApi.delete(id);
      setEvidencias(ev => ev.filter(e => e.id !== id));
      toast.success('Evidencia eliminada');
    } catch { toast.error('Error al eliminar'); }
  };

  const formatBytes = (b: number) =>
    b < 1024 ? `${b} B` : b < 1048576 ? `${(b/1024).toFixed(1)} KB` : `${(b/1048576).toFixed(1)} MB`;

  // ─── render ──────────────────────────────────────────────────────────────
  return (
    <div className="p-6 space-y-5">

      {/* Cabecera */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Hallazgos de Auditoría</h2>
          <p className="text-sm text-gray-500 mt-0.5">Motor Control Cruzado — ILG Logistics</p>
        </div>
        <button onClick={load} className="flex items-center gap-1.5 text-sm text-gray-600 hover:text-gray-900 border rounded-lg px-3 py-1.5 hover:bg-gray-50">
          <RefreshCw size={14} /> Actualizar
        </button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {[
          { label: 'Total hallazgos',  value: items.length,  color: 'text-gray-800',  bg: 'bg-gray-50'    },
          { label: 'Críticos',          value: criticos,      color: 'text-red-700',   bg: 'bg-red-50'     },
          { label: 'Abiertos',          value: abiertos,      color: 'text-orange-700',bg: 'bg-orange-50'  },
          { label: 'En proceso',        value: enProceso,     color: 'text-yellow-700',bg: 'bg-yellow-50'  },
        ].map(s => (
          <div key={s.label} className={`${s.bg} rounded-xl p-4 border`}>
            <p className="text-xs text-gray-500 font-medium">{s.label}</p>
            <p className={`text-3xl font-bold ${s.color} mt-1`}>{s.value}</p>
          </div>
        ))}
      </div>

      {/* Filtros */}
      <div className="flex flex-wrap gap-2 items-center bg-white border rounded-xl p-3">
        <Filter size={14} className="text-gray-400" />
        <input
          value={filtroBusq}
          onChange={e => setFiltroBusq(e.target.value)}
          placeholder="Buscar usuario, cédula, rol..."
          className="text-sm border rounded-lg px-3 py-1.5 w-52 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <select value={filtroCrit} onChange={e => setFiltroCrit(e.target.value)}
          className="text-sm border rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">Criticidad</option>
          <option value="CRITICA">Crítica</option>
          <option value="MEDIA">Media</option>
          <option value="BAJA">Baja</option>
        </select>
        <select value={filtroEst} onChange={e => setFiltroEst(e.target.value)}
          className="text-sm border rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">Estado</option>
          <option value="ABIERTO">Abierto</option>
          <option value="EN_PROCESO">En proceso</option>
          <option value="RESUELTO">Resuelto</option>
          <option value="CERRADO">Cerrado</option>
        </select>
        <select value={filtroTipo} onChange={e => setFiltroTipo(e.target.value)}
          className="text-sm border rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">Tipo de hallazgo</option>
          {Object.entries(TIPOS_HALLAZGO).map(([k, v]) => (
            <option key={k} value={k}>{v}</option>
          ))}
        </select>
        {(filtroCrit || filtroEst || filtroTipo || filtroBusq) && (
          <button onClick={() => { setFiltroCrit(''); setFiltroEst(''); setFiltroTipo(''); setFiltroBusq(''); }}
            className="text-xs text-red-600 hover:text-red-800 underline">
            Limpiar filtros
          </button>
        )}
        <span className="ml-auto text-xs text-gray-400">{filtered.length} de {items.length} hallazgos</span>
      </div>

      {/* Tabla */}
      <div className="bg-white rounded-xl shadow-sm border overflow-hidden">
        {loading ? (
          <div className="p-10 text-center text-gray-400">Cargando hallazgos...</div>
        ) : filtered.length === 0 ? (
          <div className="p-10 text-center text-gray-400">
            <AlertTriangle className="mx-auto mb-2 opacity-30" size={32} />
            {items.length === 0 ? 'Sin hallazgos — ejecute una simulación de Control Cruzado.' : 'No hay resultados con los filtros aplicados.'}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 w-8"></th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Estado</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Hallazgo</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Criticidad</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Usuario / Cédula</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Responsable</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Compromiso</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {filtered.map(h => {
                const expanded = expandedId === h.id;
                const cc = CRITICIDAD_CONFIG[h.criticidad];
                const tienePlan = !!h.planAccion;
                return [
                  <tr key={h.id} className={`hover:bg-gray-50 cursor-pointer ${expanded ? 'bg-blue-50/40' : ''}`}
                    onClick={() => setExpandedId(expanded ? null : h.id)}>
                    <td className="px-3 py-3 text-gray-400">
                      {expanded ? <ChevronUp size={14}/> : <ChevronDown size={14}/>}
                    </td>
                    <td className="px-4 py-3">
                      <Semaforo valor={ESTADO_SEMAFORO[h.estado] ?? 'NO_EVALUADO'} showLabel />
                    </td>
                    <td className="px-4 py-3 max-w-xs">
                      <p className="font-medium text-gray-800 truncate">{h.titulo}</p>
                      {h.tipoHallazgo && (
                        <p className="text-xs text-gray-400 mt-0.5">{TIPOS_HALLAZGO[h.tipoHallazgo] ?? h.tipoHallazgo}</p>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      {cc ? (
                        <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium ${cc.cls}`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${cc.dot}`}></span>
                          {cc.label}
                        </span>
                      ) : <span className="text-gray-400 text-xs">{h.criticidad}</span>}
                    </td>
                    <td className="px-4 py-3 text-xs text-gray-600">
                      {h.usuarioSAP && <p className="font-mono font-semibold">{h.usuarioSAP}</p>}
                      {h.cedula && <p className="text-gray-400">{h.cedula}</p>}
                      {!h.usuarioSAP && !h.cedula && <span className="text-gray-300">—</span>}
                    </td>
                    <td className="px-4 py-3 text-xs text-gray-500">{h.responsableEmail ?? '—'}</td>
                    <td className="px-4 py-3 text-xs text-gray-500">{h.fechaCompromiso ?? '—'}</td>
                    <td className="px-4 py-3" onClick={e => e.stopPropagation()}>
                      <div className="flex gap-1.5">
                        <button onClick={() => abrirPlanModal(h)}
                          className={`text-xs px-2 py-1 rounded-md border transition-colors ${tienePlan ? 'bg-green-50 text-green-700 border-green-200 hover:bg-green-100' : 'bg-blue-50 text-blue-700 border-blue-200 hover:bg-blue-100'}`}>
                          <span className="flex items-center gap-1">
                            <FileText size={11}/> {tienePlan ? 'Ver plan' : 'Plan'}
                          </span>
                        </button>
                        <button onClick={() => abrirEvidencias(h)}
                          className="text-xs px-2 py-1 rounded-md border bg-purple-50 text-purple-700 border-purple-200 hover:bg-purple-100 flex items-center gap-1">
                          <Paperclip size={11}/> Evid.
                        </button>
                        {h.estado !== 'CERRADO' && h.estado !== 'RESUELTO' && (
                          <button onClick={() => cerrar(h)}
                            className="text-xs px-2 py-1 rounded-md border bg-gray-50 text-gray-600 border-gray-200 hover:bg-gray-100 flex items-center gap-1">
                            <CheckCircle size={11}/> Cerrar
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>,

                  // ── Fila de detalle expandible ──────────────────────────
                  expanded && (
                    <tr key={`${h.id}-detail`} className="bg-blue-50/30">
                      <td colSpan={8} className="px-6 py-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">

                          {/* Descripción del hallazgo */}
                          <div>
                            <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">Descripción del hallazgo</p>
                            <p className="text-gray-700 text-xs leading-relaxed whitespace-pre-wrap">{h.descripcion}</p>

                            {(h.rolAfectado || h.transaccionesAfectadas || h.casoSESuiteRef) && (
                              <div className="mt-3 space-y-1">
                                {h.rolAfectado && (
                                  <p className="text-xs"><span className="text-gray-400">Rol:</span> <span className="font-mono text-gray-700">{h.rolAfectado}</span></p>
                                )}
                                {h.transaccionesAfectadas && (
                                  <p className="text-xs"><span className="text-gray-400">T-codes:</span> <span className="font-mono text-gray-700">{h.transaccionesAfectadas}</span></p>
                                )}
                                {h.casoSESuiteRef && (
                                  <p className="text-xs"><span className="text-gray-400">Caso SE Suite:</span> <span className="font-mono text-gray-700">{h.casoSESuiteRef}</span></p>
                                )}
                              </div>
                            )}

                            {h.normaAfectada && (
                              <div className="mt-3">
                                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">Marco normativo</p>
                                <p className="text-xs text-indigo-700 bg-indigo-50 rounded px-2 py-1">{h.normaAfectada}</p>
                              </div>
                            )}

                            {h.riesgoAsociado && (
                              <div className="mt-3">
                                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">Riesgo asociado</p>
                                <p className="text-xs text-red-700 bg-red-50 rounded px-2 py-1 leading-relaxed">{h.riesgoAsociado}</p>
                              </div>
                            )}
                          </div>

                          {/* Plan de acción recomendado */}
                          <div>
                            <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">Plan de acción</p>
                            {h.planAccion ? (
                              <pre className="text-xs text-gray-700 bg-white border rounded-lg p-3 whitespace-pre-wrap leading-relaxed max-h-52 overflow-y-auto font-sans">
                                {h.planAccion}
                              </pre>
                            ) : (
                              <p className="text-xs text-gray-400 italic">Sin plan de acción definido.</p>
                            )}
                            <div className="flex gap-2 mt-2">
                              <button onClick={() => abrirPlanModal(h)}
                                className="text-xs bg-blue-600 text-white px-3 py-1.5 rounded-lg hover:bg-blue-700">
                                {h.planAccion ? 'Editar plan' : 'Definir plan'}
                              </button>
                              <button onClick={() => abrirEvidencias(h)}
                                className="text-xs border border-purple-300 text-purple-700 px-3 py-1.5 rounded-lg hover:bg-purple-50 flex items-center gap-1">
                                <Paperclip size={11}/> Evidencias
                              </button>
                            </div>
                          </div>
                        </div>
                      </td>
                    </tr>
                  )
                ];
              })}
            </tbody>
          </table>
        )}
      </div>

      {/* ── Modal: Plan de acción ──────────────────────────────────────────── */}
      {planModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl w-full max-w-2xl shadow-2xl flex flex-col max-h-[90vh]">
            <div className="flex items-center justify-between p-5 border-b">
              <div>
                <h3 className="font-semibold text-gray-900">Plan de Acción</h3>
                <p className="text-xs text-gray-500 mt-0.5">{planModal.titulo}</p>
              </div>
              <button onClick={() => setPlanModal(null)} className="text-gray-400 hover:text-gray-600"><X size={18}/></button>
            </div>

            <div className="p-5 flex-1 overflow-y-auto space-y-4">
              {/* Metadatos del hallazgo */}
              {planModal.normaAfectada && (
                <div className="bg-indigo-50 rounded-lg px-3 py-2 text-xs text-indigo-700">
                  <span className="font-semibold">Norma: </span>{planModal.normaAfectada}
                </div>
              )}

              {/* Plan editable */}
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1.5">
                  Plan de acción correctiva <span className="text-red-500">*</span>
                </label>
                <p className="text-xs text-gray-400 mb-2">
                  El plan ha sido pre-rellenado con la recomendación del motor de auditoría. Puede ajustarlo según el contexto de ILG Logistics.
                </p>
                <textarea
                  value={planText}
                  onChange={e => setPlanText(e.target.value)}
                  rows={10}
                  className="w-full border rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Describe el plan de acción correctiva..."
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1.5">Responsable (email)</label>
                  <input
                    type="email"
                    value={planResponsable}
                    onChange={e => setPlanResponsable(e.target.value)}
                    placeholder="responsable@ilglogistics.com"
                    className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1.5">Fecha compromiso</label>
                  <input
                    type="date"
                    value={planFecha}
                    onChange={e => setPlanFecha(e.target.value)}
                    className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>
            </div>

            <div className="p-5 border-t flex justify-end gap-2">
              <button onClick={() => setPlanModal(null)} className="border px-4 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-50">
                Cancelar
              </button>
              <button onClick={guardarPlan} disabled={savingPlan || !planText.trim()}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2">
                {savingPlan ? <><RefreshCw size={14} className="animate-spin"/> Guardando...</> : 'Guardar plan'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Modal: Evidencias ──────────────────────────────────────────────── */}
      {evidModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl w-full max-w-xl shadow-2xl flex flex-col max-h-[85vh]">
            <div className="flex items-center justify-between p-5 border-b">
              <div>
                <h3 className="font-semibold text-gray-900">Evidencias del hallazgo</h3>
                <p className="text-xs text-gray-500 mt-0.5 truncate max-w-xs">{evidModal.titulo}</p>
              </div>
              <button onClick={() => setEvidModal(null)} className="text-gray-400 hover:text-gray-600"><X size={18}/></button>
            </div>

            <div className="p-5 flex-1 overflow-y-auto space-y-4">
              {/* Upload zone */}
              <div
                className="border-2 border-dashed border-gray-300 rounded-xl p-5 text-center cursor-pointer hover:border-blue-400 hover:bg-blue-50/30 transition-colors"
                onClick={() => fileRef.current?.click()}
                onDragOver={e => e.preventDefault()}
                onDrop={e => {
                  e.preventDefault();
                  const f = e.dataTransfer.files[0];
                  if (f) subirEvidencia(f);
                }}
              >
                <Upload className="mx-auto text-gray-400 mb-2" size={28}/>
                <p className="text-sm text-gray-600 font-medium">Arrastra archivos o haz clic para seleccionar</p>
                <p className="text-xs text-gray-400 mt-1">PDF, Word, Excel, CSV, PNG, JPG — máx. 50 MB</p>
                <input ref={fileRef} type="file" className="hidden"
                  accept=".pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg"
                  onChange={e => { const f = e.target.files?.[0]; if (f) subirEvidencia(f); e.target.value = ''; }}
                />
              </div>

              <input
                value={descEvid}
                onChange={e => setDescEvid(e.target.value)}
                placeholder="Descripción del archivo (opcional)..."
                className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />

              {uploadingEvid && (
                <div className="flex items-center gap-2 text-sm text-blue-600">
                  <RefreshCw size={14} className="animate-spin"/> Subiendo...
                </div>
              )}

              {/* Lista de evidencias */}
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
                  Archivos adjuntos ({evidencias.length})
                </p>
                {loadingEvid ? (
                  <p className="text-sm text-gray-400">Cargando...</p>
                ) : evidencias.length === 0 ? (
                  <p className="text-sm text-gray-400 italic">Sin evidencias adjuntas.</p>
                ) : (
                  <div className="space-y-2">
                    {evidencias.map(ev => (
                      <div key={ev.id} className="flex items-center gap-3 border rounded-lg px-3 py-2">
                        <FileText className="text-blue-400 shrink-0" size={20}/>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-gray-800 truncate">{ev.nombreArchivo}</p>
                          <p className="text-xs text-gray-400">
                            {ev.tipoEvidencia} · {formatBytes(ev.tamanoBytes)} · {ev.subidoPor}
                          </p>
                        </div>
                        <div className="flex gap-1.5 shrink-0">
                          {ev.sasUrl && (
                            <a href={ev.sasUrl} target="_blank" rel="noopener noreferrer"
                              className="p-1.5 text-blue-600 hover:bg-blue-50 rounded" title="Descargar">
                              <Download size={14}/>
                            </a>
                          )}
                          <button onClick={() => eliminarEvidencia(ev.id)}
                            className="p-1.5 text-red-400 hover:bg-red-50 rounded" title="Eliminar">
                            <X size={14}/>
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>

            <div className="p-4 border-t flex justify-end">
              <button onClick={() => setEvidModal(null)} className="border px-4 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-50">
                Cerrar
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
