import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  getSimulaciones, iniciarSimulacion, cancelarSimulacion,
  ejecutarControlCruzado,
  type SimulacionListDto, type ControlCruzadoResultado
} from '../api/simulaciones';
import { toast } from 'sonner';
import { PlayCircle, XCircle, ChevronRight, ShieldAlert, RefreshCw, X, FileSearch } from 'lucide-react';
import { useForm } from 'react-hook-form';

const estadoColor: Record<string, string> = {
  COMPLETADA: 'bg-green-100 text-green-800',
  EN_PROCESO: 'bg-blue-100 text-blue-800',
  PENDIENTE: 'bg-yellow-100 text-yellow-800',
  ERROR: 'bg-red-100 text-red-800',
  CANCELADA: 'bg-gray-100 text-gray-700',
};

const TIPOS_CONTROL = [
  { value: 'COMPLETO',     label: 'Completo (todas las reglas)' },
  { value: 'SAP_NOMINA',   label: 'SAP ↔ Nómina' },
  { value: 'SOD_ONLY',     label: 'Solo SoD (conflictos)' },
];

export function Simulaciones() {
  const [items, setItems] = useState<SimulacionListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [ejecutandoId, setEjecutandoId] = useState<string | null>(null);
  const [showControlModal, setShowControlModal] = useState<string | null>(null);
  const [controlResultado, setControlResultado] = useState<ControlCruzadoResultado | null>(null);
  const [objetivo, setObjetivo] = useState('');
  const [tipoControl, setTipoControl] = useState<string>('COMPLETO');
  const hoy = new Date().toISOString().split('T')[0];
  const nombreSugerido = (() => {
    const d = new Date();
    const meses = ['Enero','Febrero','Marzo','Abril','Mayo','Junio','Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre'];
    return `Auditoría de Accesos SAP — ${meses[d.getMonth()]} ${d.getFullYear()}`;
  })();
  const { register, handleSubmit, reset } = useForm<{ nombre: string; periodoInicio: string; periodoFin: string }>({
    defaultValues: { nombre: nombreSugerido, periodoInicio: hoy, periodoFin: hoy },
  });
  const navigate = useNavigate();

  const load = () => {
    getSimulaciones()
      .then((r) => setItems(r.items))
      .catch(() => toast.error('Error al cargar simulaciones'))
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const onSubmit = async (data: { nombre: string; periodoInicio: string; periodoFin: string }) => {
    try {
      await iniciarSimulacion({
        nombre: data.nombre,
        tipo: 'MANUAL',
        sociedadIds: [1],
        periodoInicio: data.periodoInicio,
        periodoFin: data.periodoFin,
      });
      toast.success('Simulación creada');
      setShowForm(false);
      reset();
      load();
    } catch {
      toast.error('Error al iniciar la simulación');
    }
  };

  const cancelar = async (id: string) => {
    try {
      await cancelarSimulacion(id);
      toast.success('Simulación cancelada');
      load();
    } catch {
      toast.error('Error al cancelar');
    }
  };

  const abrirControlModal = (id: string) => {
    setShowControlModal(id);
    setControlResultado(null);
    setObjetivo('');
    setTipoControl('COMPLETO');
  };

  const ejecutarControl = async () => {
    if (!showControlModal) return;
    setEjecutandoId(showControlModal);
    try {
      const res = await ejecutarControlCruzado(showControlModal, {
        objetivo: objetivo || undefined,
        tipoControlCruzado: tipoControl as 'COMPLETO',
      });
      setControlResultado(res);
      toast.success(`Control cruzado completado: ${res.totalHallazgos} hallazgos`);
      load();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      if (msg) {
        toast.error(msg);
        setShowControlModal(null);
      } else {
        toast.error('Error al ejecutar el motor de control cruzado');
      }
    } finally {
      setEjecutandoId(null);
    }
  };

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Simulaciones de Auditoría</h2>
        <button
          onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm hover:bg-blue-700 transition"
        >
          <PlayCircle size={16} />
          Nueva Simulación
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="bg-white rounded-xl border p-4 space-y-3">
          <h3 className="font-semibold text-gray-700">Nueva Simulación</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div>
              <label className="text-xs text-gray-500">Nombre de la simulación</label>
              <input
                {...register('nombre', { required: true })}
                placeholder="Ej: Auditoría de Accesos SAP — Abril 2026"
                className="w-full border rounded-lg px-3 py-2 text-sm mt-1"
              />
              <p className="text-[11px] text-gray-400 mt-0.5">Puede editar el nombre sugerido</p>
            </div>
            <div>
              <label className="text-xs text-gray-500">Período inicio</label>
              <input type="date" {...register('periodoInicio', { required: true })} className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
            </div>
            <div>
              <label className="text-xs text-gray-500">Período fin</label>
              <input type="date" {...register('periodoFin', { required: true })} className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
            </div>
          </div>
          <div className="flex gap-2">
            <button type="submit" className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm hover:bg-blue-700">
              Crear
            </button>
            <button type="button" onClick={() => setShowForm(false)} className="border px-4 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-50">
              Cancelar
            </button>
          </div>
        </form>
      )}

      {/* Modal Motor de Control Cruzado */}
      {showControlModal && (
        <div className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <div className="flex items-center gap-2">
                <ShieldAlert size={20} className="text-violet-600" />
                <h3 className="font-semibold text-gray-800">Motor de Control Cruzado</h3>
              </div>
              <button onClick={() => setShowControlModal(null)} className="text-gray-400 hover:text-gray-600">
                <X size={18} />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="text-xs font-medium text-gray-600 mb-1 block">Objetivo / comentario del auditor</label>
                <textarea
                  value={objetivo}
                  onChange={e => setObjetivo(e.target.value)}
                  rows={3}
                  placeholder="Ej: Recertificación semestral accesos SAP — Q1 2025"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500"
                />
              </div>
              <div>
                <label className="text-xs font-medium text-gray-600 mb-1 block">Tipo de control cruzado</label>
                <select
                  value={tipoControl}
                  onChange={e => setTipoControl(e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500"
                >
                  {TIPOS_CONTROL.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </div>

              {/* Descripción de reglas */}
              <div className="bg-violet-50 rounded-xl p-4 space-y-1.5">
                <p className="text-xs font-semibold text-violet-700 mb-2">Reglas que se ejecutarán:</p>
                {[
                  { id: 'R01', label: 'Ex-empleado con usuario SAP activo' },
                  { id: 'R02', label: 'Conflicto de Segregación de Funciones (SoD)' },
                  { id: 'R03', label: 'Rol fuera de Matriz de Puestos sin caso SE Suite vigente' },
                  { id: 'R04', label: 'Caso SE Suite vencido — acceso no renovado' },
                  { id: 'R05', label: 'Empleado activo sin cuenta SAP vinculada por cédula' },
                ].filter(r => tipoControl === 'COMPLETO' ||
                  (tipoControl === 'SAP_NOMINA' && ['R01','R03','R04','R05'].includes(r.id)) ||
                  (tipoControl === 'SOD_ONLY' && r.id === 'R02')
                ).map(r => (
                  <div key={r.id} className="flex items-center gap-2 text-xs text-violet-800">
                    <span className="font-mono bg-violet-200 px-1.5 py-0.5 rounded text-violet-700">{r.id}</span>
                    <span>{r.label}</span>
                  </div>
                ))}
              </div>

              {/* Resultado */}
              {controlResultado && (
                <div className="bg-green-50 border border-green-200 rounded-xl p-4">
                  <p className="text-sm font-semibold text-green-800 mb-3">
                    Ejecución completada — {controlResultado.totalHallazgos} hallazgos generados
                  </p>
                  <div className="grid grid-cols-3 gap-2 mb-3">
                    <div className="bg-white rounded-lg px-3 py-2 text-center shadow-sm">
                      <p className="text-xs text-gray-500">Críticos</p>
                      <p className="text-lg font-bold text-red-600">{controlResultado.criticos}</p>
                    </div>
                    <div className="bg-white rounded-lg px-3 py-2 text-center shadow-sm">
                      <p className="text-xs text-gray-500">Medios</p>
                      <p className="text-lg font-bold text-yellow-600">{controlResultado.medios}</p>
                    </div>
                    <div className="bg-white rounded-lg px-3 py-2 text-center shadow-sm">
                      <p className="text-xs text-gray-500">Bajos</p>
                      <p className="text-lg font-bold text-blue-600">{controlResultado.bajos}</p>
                    </div>
                  </div>
                  <div className="space-y-1">
                    {Object.entries(controlResultado.porRegla).map(([regla, count]) => (
                      <div key={regla} className="flex items-center justify-between text-xs">
                        <span className="text-gray-600 font-mono">{regla}</span>
                        <span className={`font-semibold ${count > 0 ? 'text-red-600' : 'text-green-600'}`}>
                          {count} hallazgo{count !== 1 ? 's' : ''}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
            <div className="px-6 py-4 border-t flex gap-3">
              {!controlResultado ? (
                <button
                  onClick={ejecutarControl}
                  disabled={!!ejecutandoId}
                  className="flex items-center gap-2 bg-violet-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-violet-700 disabled:opacity-50 transition"
                >
                  {ejecutandoId
                    ? <><RefreshCw size={14} className="animate-spin" /> Ejecutando...</>
                    : <><ShieldAlert size={14} /> Ejecutar Motor</>}
                </button>
              ) : (
                <button
                  onClick={() => navigate(`/hallazgos?simulacionId=${showControlModal}`)}
                  className="bg-violet-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-violet-700"
                >
                  Ver Hallazgos
                </button>
              )}
              <button onClick={() => setShowControlModal(null)}
                className="border px-4 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-50">
                Cerrar
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="bg-white rounded-xl shadow-sm border overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-400">Cargando...</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600">Nombre</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600">Estado</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600">Score</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600">Cumplimiento</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600">Rojo</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600">Iniciada</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {items.map((s) => (
                <tr key={s.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-800 cursor-pointer"
                      onClick={() => navigate(`/simulaciones/${s.id}`)}>
                    {s.nombre}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${estadoColor[s.estado] ?? 'bg-gray-100'}`}>
                      {s.estado}
                    </span>
                  </td>
                  <td className="px-4 py-3">{s.scoreMadurez?.toFixed(1) ?? '—'}</td>
                  <td className="px-4 py-3">{s.porcentajeCumplimiento ? `${s.porcentajeCumplimiento.toFixed(1)}%` : '—'}</td>
                  <td className="px-4 py-3 text-red-600 font-medium">{s.controlesRojo ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-500">{new Date(s.iniciadaAt).toLocaleDateString('es-CR')}</td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2 justify-end">
                      {/* COMPLETADA: solo ver hallazgos — no se puede re-ejecutar */}
                      {s.estado === 'COMPLETADA' && (
                        <button
                          title="Ver Hallazgos"
                          onClick={e => { e.stopPropagation(); navigate(`/hallazgos?simulacionId=${s.id}`); }}
                          className="text-green-500 hover:text-green-700 transition"
                        >
                          <FileSearch size={15} />
                        </button>
                      )}
                      {/* Motor de Control Cruzado — solo disponible en PENDIENTE */}
                      {s.estado === 'PENDIENTE' && (
                        <button
                          title="Ejecutar Motor de Control Cruzado"
                          onClick={e => { e.stopPropagation(); abrirControlModal(s.id); }}
                          className="text-violet-400 hover:text-violet-700 transition"
                        >
                          <ShieldAlert size={15} />
                        </button>
                      )}
                      {(s.estado === 'PENDIENTE' || s.estado === 'EN_PROCESO') && (
                        <button onClick={e => { e.stopPropagation(); cancelar(s.id); }} className="text-red-400 hover:text-red-700">
                          <XCircle size={15} />
                        </button>
                      )}
                      <ChevronRight size={14} className="text-gray-300 cursor-pointer"
                        onClick={() => navigate(`/simulaciones/${s.id}`)} />
                    </div>
                  </td>
                </tr>
              ))}
              {items.length === 0 && (
                <tr><td colSpan={7} className="px-4 py-8 text-center text-gray-400">Sin simulaciones registradas</td></tr>
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
