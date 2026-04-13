import { useState, useRef, useEffect } from 'react';
import { cargasApi, type CargaResultado, type SnapshotEntraIDDto } from '../api/cargas';
import { toast } from 'sonner';
import {
  Upload, Download, FileSpreadsheet, Users, ShieldCheck,
  CheckCircle2, XCircle, AlertTriangle, RefreshCw, X, LayoutGrid, Briefcase,
  MonitorSmartphone, Calendar, Hash, DatabaseZap
} from 'lucide-react';

type TipoCarga = 'empleados' | 'sapRoles' | 'matrizPuestos' | 'casosSeSuite' | 'entraID';

interface CargaState {
  file: File | null;
  resultado: CargaResultado | null;
  loading: boolean;
  dragging: boolean;
}

const TIPO_CONFIG: Record<TipoCarga, {
  label: string;
  descripcion: string;
  icon: React.ReactNode;
  columnas: string[];
}> = {
  empleados: {
    label: 'Empleados Maestro',
    descripcion: 'Importa o actualiza el padrón de empleados desde nómina (SE Suite / Evolution HR).',
    icon: <Users size={20} className="text-blue-600" />,
    columnas: ['NumeroEmpleado*', 'Nombre*', 'ApellidoPaterno*', 'ApellidoMaterno', 'CorreoCorporativo',
               'FechaIngreso (YYYY-MM-DD)', 'EstadoLaboral (ACTIVO/INACTIVO/BAJA_PROCESADA)',
               'DepartamentoCodigo', 'PuestoCodigo'],
  },
  sapRoles: {
    label: 'SAP — Usuarios, Roles y Transacciones',
    descripcion: 'Exporta V_SAP_USR_RECERTIFICAION y súbelo directamente. Una fila por usuario+rol+transacción.',
    icon: <ShieldCheck size={20} className="text-emerald-600" />,
    columnas: [
      'ID (Cédula)', 'USUARIO*', 'NOMBRE_COMPLETO', 'SOCIEDAD', 'DEPARTAMENTO', 'PUESTO',
      'EMAIL', 'ROL*', 'INICIO_VALIDEZ (DD/MM/YYYY)', 'FIN_VALIDEZ (DD/MM/YYYY)',
      'TRANSACCION*', 'ULTIMO_INGRESO',
    ],
  },
  matrizPuestos: {
    label: 'Matriz de Puestos (Contraloría)',
    descripcion: 'Matriz oficial de roles y transacciones aprobados por Contraloría para cada puesto.',
    icon: <LayoutGrid size={20} className="text-violet-600" />,
    columnas: [
      'ID (Cédula)', 'USUARIO*', 'NOMBRE_COMPLETO', 'SOCIEDAD', 'DEPARTAMENTO', 'PUESTO*',
      'EMAIL', 'ROL*', 'INICIO_VALIDEZ (DD/MM/YYYY)', 'FIN_VALIDEZ (DD/MM/YYYY)',
      'TRANSACCION*', 'ULTIMO_INGRESO', 'FECHA_REVISION_CONTRALORIA',
    ],
  },
  casosSeSuite: {
    label: 'Casos SE Suite (justificaciones)',
    descripcion: 'Casos aprobados que justifican accesos fuera de la Matriz de Puestos.',
    icon: <Briefcase size={20} className="text-amber-600" />,
    columnas: [
      'NUMERO_CASO*', 'TITULO', 'USUARIO_SAP*', 'CEDULA',
      'ROL_JUSTIFICADO*', 'TRANSACCIONES (separadas por coma)',
      'FECHA_APROBACION (DD/MM/YYYY)', 'FECHA_VENCIMIENTO (DD/MM/YYYY)',
      'ESTADO (APROBADO/VENCIDO/ANULADO)', 'APROBADOR',
    ],
  },
  entraID: {
    label: 'Entra ID',
    descripcion: 'Snapshot del directorio Azure AD. Cada carga queda grabada como punto en el tiempo con fecha exacta.',
    icon: <MonitorSmartphone size={20} className="text-sky-600" />,
    columnas: [
      'EmployeeId* (Cédula — campo clave)', 'ObjectId', 'DisplayName*',
      'UserPrincipalName*', 'Email', 'Department', 'JobTitle',
      'AccountEnabled (TRUE/FALSE)', 'Manager', 'OfficeLocation',
      'CreatedDateTime', 'LastSignInDateTime',
    ],
  },
};

function DropZone({
  onFile, file, onClear, loading
}: {
  onFile: (f: File) => void;
  file: File | null;
  onClear: () => void;
  loading: boolean;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault(); setDragging(false);
    const f = e.dataTransfer.files[0];
    if (f) onFile(f);
  };

  return (
    <div
      onDrop={handleDrop}
      onDragOver={e => { e.preventDefault(); setDragging(true); }}
      onDragLeave={() => setDragging(false)}
      onClick={() => !file && inputRef.current?.click()}
      className={`relative border-2 border-dashed rounded-xl p-8 text-center transition-all cursor-pointer
        ${dragging ? 'border-blue-400 bg-blue-50' : file ? 'border-green-300 bg-green-50 cursor-default' : 'border-gray-300 hover:border-blue-400 hover:bg-gray-50'}`}
    >
      <input ref={inputRef} type="file" className="hidden"
        accept=".xlsx,.xls,.csv"
        onChange={e => { const f = e.target.files?.[0]; if (f) onFile(f); e.target.value = ''; }} />

      {file ? (
        <div className="flex items-center justify-center gap-3">
          <FileSpreadsheet size={28} className="text-green-600" />
          <div className="text-left">
            <p className="font-medium text-green-700">{file.name}</p>
            <p className="text-xs text-green-600">{(file.size / 1024).toFixed(1)} KB</p>
          </div>
          <button onClick={e => { e.stopPropagation(); onClear(); }}
            className="ml-2 text-gray-400 hover:text-red-500 transition">
            <X size={16} />
          </button>
        </div>
      ) : (
        <>
          <Upload size={32} className="mx-auto text-gray-400 mb-3" />
          <p className="font-medium text-gray-700">Arrastra el archivo aquí</p>
          <p className="text-sm text-gray-500 mt-1">o <span className="text-blue-600 underline">haz clic para seleccionar</span></p>
          <p className="text-xs text-gray-400 mt-2">Excel (.xlsx) o CSV · máx 10 MB</p>
        </>
      )}
      {loading && (
        <div className="absolute inset-0 bg-white/70 rounded-xl flex items-center justify-center">
          <RefreshCw size={24} className="animate-spin text-blue-600" />
          <span className="ml-2 text-sm text-blue-600 font-medium">Procesando...</span>
        </div>
      )}
    </div>
  );
}

function ResultadoPanel({ resultado, onClose }: { resultado: CargaResultado; onClose: () => void }) {
  const exitoso = resultado.errores === 0;
  return (
    <div className={`rounded-xl border p-5 ${exitoso ? 'bg-green-50 border-green-200' : 'bg-yellow-50 border-yellow-200'}`}>
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center gap-2">
          {exitoso
            ? <CheckCircle2 size={20} className="text-green-600" />
            : <AlertTriangle size={20} className="text-yellow-600" />}
          <h3 className="font-semibold text-gray-800">
            {exitoso ? 'Carga completada sin errores' : `Carga completada con ${resultado.errores} error(es)`}
          </h3>
        </div>
        <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
          <X size={16} />
        </button>
      </div>

      <div className="grid grid-cols-4 gap-3 mb-4">
        {[
          { label: 'Total filas', value: resultado.totalRegistros, color: 'text-gray-700' },
          { label: 'Insertados', value: resultado.insertados,      color: 'text-green-700' },
          { label: 'Actualizados', value: resultado.actualizados,  color: 'text-blue-700' },
          { label: 'Errores',    value: resultado.errores,         color: resultado.errores > 0 ? 'text-red-700' : 'text-gray-400' },
        ].map(s => (
          <div key={s.label} className="bg-white rounded-lg px-3 py-2 text-center shadow-sm">
            <p className="text-xs text-gray-500">{s.label}</p>
            <p className={`text-xl font-bold mt-0.5 ${s.color}`}>{s.value}</p>
          </div>
        ))}
      </div>

      {resultado.detalleErrores.length > 0 && (
        <div>
          <p className="text-xs font-semibold text-red-700 mb-2 flex items-center gap-1">
            <XCircle size={13} /> Detalle de errores ({resultado.detalleErrores.length})
          </p>
          <div className="bg-white rounded-lg border border-red-200 max-h-48 overflow-y-auto">
            {resultado.detalleErrores.map((e, i) => (
              <div key={i} className={`px-3 py-2 text-xs text-red-700 ${i > 0 ? 'border-t border-red-100' : ''}`}>
                {e}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Page
// ─────────────────────────────────────────────────────────────────────────────
const SISTEMAS = ['SAP', 'EVOLUTION', 'SE_SUITE', 'AD', 'OTRO'];

export function Cargas() {
  const [tipo, setTipo] = useState<TipoCarga>('empleados');
  const [sistema, setSistema] = useState('SAP');
  const [state, setState] = useState<CargaState>({ file: null, resultado: null, loading: false, dragging: false });
  const [nombreSnapshot, setNombreSnapshot] = useState('');
  const [snapshots, setSnapshots] = useState<SnapshotEntraIDDto[]>([]);
  const [loadingSnapshots, setLoadingSnapshots] = useState(false);
  const [descargandoId, setDescargandoId] = useState<string | null>(null);
  const [ultimoSnapshot, setUltimoSnapshot] = useState<{ id: string; nombre: string } | null>(null);
  const cfg = TIPO_CONFIG[tipo];

  const setFile = (file: File) => setState(s => ({ ...s, file, resultado: null }));
  const clearFile = () => setState(s => ({ ...s, file: null, resultado: null }));
  const clearResultado = () => { setState(s => ({ ...s, resultado: null })); setUltimoSnapshot(null); };

  // Cargar historial de snapshots cuando se selecciona Entra ID
  useEffect(() => {
    if (tipo === 'entraID') {
      setLoadingSnapshots(true);
      cargasApi.getSnapshotsEntraID()
        .then(setSnapshots)
        .catch(() => toast.error('Error al cargar historial de snapshots'))
        .finally(() => setLoadingSnapshots(false));
    }
  }, [tipo]);

  const handleCarga = async () => {
    const archivo = state.file;
    if (!archivo) { toast.error('Selecciona un archivo primero'); return; }
    setState(s => ({ ...s, loading: true, resultado: null }));
    try {
      if (tipo === 'entraID') {
        const res = await cargasApi.cargarSnapshotEntraID(archivo, nombreSnapshot || undefined);
        // Convertir resultado a CargaResultado para usar el mismo panel
        const resultado: CargaResultado = {
          totalRegistros: res.totalRegistros,
          insertados: res.totalRegistros - res.errores,
          actualizados: 0,
          errores: res.errores,
          detalleErrores: res.detalleErrores,
        };
        setState(s => ({ ...s, resultado, file: null }));
        setUltimoSnapshot({ id: res.snapshotId, nombre: res.nombre });
        if (res.errores === 0)
          toast.success(`Snapshot "${res.nombre}" insertado: ${res.totalRegistros} usuarios`);
        else
          toast.warning(`Snapshot insertado con ${res.errores} error(es)`);
        // Refrescar lista de snapshots
        cargasApi.getSnapshotsEntraID().then(setSnapshots).catch(() => {});
        setNombreSnapshot('');
        return;
      }

      const resultado =
        tipo === 'empleados'     ? await cargasApi.cargarEmpleados(archivo, 1) :
        tipo === 'sapRoles'      ? await cargasApi.cargarRolesSAP(archivo, sistema) :
        tipo === 'matrizPuestos' ? await cargasApi.cargarMatrizPuestos(archivo) :
                                   await cargasApi.cargarCasosSeSuite(archivo);
      setState(s => ({ ...s, resultado, file: null }));
      if (resultado.errores === 0)
        toast.success(`Carga completada: ${resultado.insertados} nuevos, ${resultado.actualizados} actualizados`);
      else
        toast.warning(`Carga con errores: ${resultado.errores} filas no procesadas`);
    } catch {
      toast.error('Error al procesar el archivo');
    } finally {
      setState(s => ({ ...s, loading: false }));
    }
  };

  const descargarPlantilla = async () => {
    try {
      const fn =
        tipo === 'empleados'     ? cargasApi.descargarPlantillaEmpleados :
        tipo === 'sapRoles'      ? cargasApi.descargarPlantillaSapRoles :
        tipo === 'matrizPuestos' ? cargasApi.descargarPlantillaMatrizPuestos :
        tipo === 'entraID'       ? cargasApi.descargarPlantillaEntraID :
                                   cargasApi.descargarPlantillaCasosSeSuite;
      await fn();
    } catch {
      toast.error('Error al descargar la plantilla');
    }
  };

  const descargarSnapshot = async (snap: SnapshotEntraIDDto) => {
    setDescargandoId(snap.id);
    try {
      await cargasApi.descargarSnapshotEntraID(snap.id, snap.nombre);
      toast.success(`Descargando "${snap.nombre}"`);
    } catch {
      toast.error('Error al descargar el snapshot');
    } finally {
      setDescargandoId(null);
    }
  };

  return (
    <div className="p-6 max-w-3xl">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Cargas Masivas</h1>
        <p className="text-sm text-gray-500 mt-0.5">Importa registros desde archivos Excel o CSV</p>
      </div>

      {/* Selector de tipo */}
      <div className="flex gap-3 mb-6">
        {(Object.entries(TIPO_CONFIG) as [TipoCarga, typeof TIPO_CONFIG[TipoCarga]][]).map(([key, c]) => (
          <button key={key} onClick={() => { setTipo(key); clearFile(); clearResultado(); setUltimoSnapshot(null); }}
            className={`flex-1 flex items-start gap-3 p-4 rounded-xl border transition text-left ${
              tipo === key ? 'border-blue-500 bg-blue-50' : 'border-gray-200 bg-white hover:border-gray-300'}`}>
            {c.icon}
            <div>
              <p className={`font-semibold text-sm ${tipo === key ? 'text-blue-800' : 'text-gray-700'}`}>{c.label}</p>
              <p className="text-xs text-gray-500 mt-0.5">{c.descripcion}</p>
            </div>
          </button>
        ))}
      </div>

      {/* Columnas de la plantilla */}
      <div className="bg-gray-50 rounded-xl border border-gray-200 p-4 mb-5">
        <div className="flex items-center justify-between mb-2">
          <p className="text-xs font-semibold text-gray-600">Columnas requeridas en la plantilla</p>
          <button onClick={descargarPlantilla}
            className="flex items-center gap-1.5 text-xs text-blue-600 hover:underline">
            <Download size={12} /> Descargar plantilla .xlsx
          </button>
        </div>
        <div className="flex flex-wrap gap-1.5">
          {cfg.columnas.map(col => (
            <span key={col}
              className={`px-2 py-0.5 rounded text-[11px] font-mono ${
                col.endsWith('*') ? 'bg-blue-100 text-blue-700' : 'bg-gray-100 text-gray-600'}`}>
              {col}
            </span>
          ))}
        </div>
        <p className="text-[11px] text-gray-400 mt-2">* Campos obligatorios</p>
      </div>

      {/* Selector de sistema (para SAP roles) */}
      {tipo === 'sapRoles' && (
        <div className="mb-4 flex items-center gap-3">
          <label className="text-sm font-medium text-gray-700 whitespace-nowrap">Sistema origen:</label>
          <select
            value={sistema}
            onChange={e => setSistema(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {SISTEMAS.map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
      )}

      {/* Configuración del snapshot (sólo Entra ID) */}
      {tipo === 'entraID' && (
        <div className="mb-4 bg-sky-50 border border-sky-200 rounded-xl p-4 space-y-3">
          <div className="flex items-center gap-2 text-sky-700 font-semibold text-sm">
            <DatabaseZap size={16} />
            Configurar snapshot
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Nombre del snapshot <span className="text-gray-400 font-normal">(opcional)</span>
            </label>
            <input
              type="text"
              value={nombreSnapshot}
              onChange={e => setNombreSnapshot(e.target.value)}
              placeholder='Ej: "Cierre Q1 2025" o "Auditoría Marzo"'
              className="w-full border border-sky-300 bg-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 placeholder:text-gray-400"
            />
          </div>
          <div className="flex items-center gap-2 text-xs text-gray-500">
            <Calendar size={12} className="text-sky-500 flex-shrink-0" />
            Fecha del snapshot:
            <span className="font-semibold text-gray-700 tabular-nums">
              {new Date().toLocaleString('es-CR', {
                day: '2-digit', month: 'short', year: 'numeric',
                hour: '2-digit', minute: '2-digit', second: '2-digit',
              })}
            </span>
            <span className="text-gray-400">(se grabará al momento de insertar)</span>
          </div>
        </div>
      )}

      {/* Drop zone */}
      <DropZone
        onFile={setFile}
        file={state.file}
        onClear={clearFile}
        loading={state.loading}
      />

      {/* Botón procesar */}
      {state.file && (
        <div className="mt-4 flex items-center gap-3">
          {tipo === 'entraID' ? (
            <button onClick={handleCarga} disabled={state.loading}
              className="flex items-center gap-2 bg-sky-600 text-white px-5 py-2.5 rounded-lg text-sm font-medium hover:bg-sky-700 disabled:opacity-50 transition">
              {state.loading ? <RefreshCw size={15} className="animate-spin" /> : <DatabaseZap size={15} />}
              {state.loading ? 'Insertando...' : 'Insertar en Base de Datos'}
            </button>
          ) : (
            <button onClick={handleCarga} disabled={state.loading}
              className="flex items-center gap-2 bg-blue-600 text-white px-5 py-2.5 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition">
              {state.loading ? <RefreshCw size={15} className="animate-spin" /> : <Upload size={15} />}
              {state.loading ? 'Procesando...' : `Cargar ${cfg.label}`}
            </button>
          )}
          <button onClick={clearFile} className="text-sm text-gray-500 hover:text-gray-700 transition">
            Cancelar
          </button>
        </div>
      )}

      {/* Resultado */}
      {state.resultado && (
        <div className="mt-5 space-y-3">
          <ResultadoPanel resultado={state.resultado} onClose={clearResultado} />
          {/* Descarga inmediata del snapshot recién insertado */}
          {tipo === 'entraID' && ultimoSnapshot && (
            <div className="flex items-center gap-3 bg-sky-50 border border-sky-200 rounded-xl px-4 py-3">
              <CheckCircle2 size={16} className="text-sky-600 flex-shrink-0" />
              <div className="flex-1 text-sm text-sky-800">
                <span className="font-semibold">"{ultimoSnapshot.nombre}"</span> insertado correctamente en la base de datos.
              </div>
              <button
                onClick={() => descargarSnapshot({ id: ultimoSnapshot.id, nombre: ultimoSnapshot.nombre, fechaInstantanea: new Date().toISOString(), totalRegistros: state.resultado!.totalRegistros })}
                disabled={descargandoId === ultimoSnapshot.id}
                className="flex items-center gap-1.5 bg-sky-600 text-white px-3 py-1.5 rounded-lg text-xs font-medium hover:bg-sky-700 disabled:opacity-50 transition flex-shrink-0"
              >
                {descargandoId === ultimoSnapshot.id
                  ? <RefreshCw size={13} className="animate-spin" />
                  : <Download size={13} />}
                Descargar Excel
              </button>
            </div>
          )}
        </div>
      )}

      {/* Historial de snapshots Entra ID */}
      {tipo === 'entraID' && (
        <div className="mt-8">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-semibold text-gray-700 flex items-center gap-2">
              <Calendar size={15} className="text-sky-600" />
              Historial de snapshots
            </h2>
            <button
              onClick={() => {
                setLoadingSnapshots(true);
                cargasApi.getSnapshotsEntraID()
                  .then(setSnapshots)
                  .catch(() => toast.error('Error al refrescar historial'))
                  .finally(() => setLoadingSnapshots(false));
              }}
              className="flex items-center gap-1 text-xs text-gray-500 hover:text-blue-600 transition"
            >
              <RefreshCw size={12} className={loadingSnapshots ? 'animate-spin' : ''} />
              Refrescar
            </button>
          </div>

          {loadingSnapshots ? (
            <div className="flex items-center justify-center py-10 text-gray-400 text-sm gap-2">
              <RefreshCw size={16} className="animate-spin" /> Cargando historial...
            </div>
          ) : snapshots.length === 0 ? (
            <div className="text-center py-10 text-gray-400 text-sm border border-dashed border-gray-200 rounded-xl">
              No hay snapshots cargados todavía.
            </div>
          ) : (
            <div className="border border-gray-200 rounded-xl overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="text-left px-4 py-2.5 text-xs font-semibold text-gray-600">Nombre</th>
                    <th className="text-left px-4 py-2.5 text-xs font-semibold text-gray-600">
                      <span className="flex items-center gap-1"><Calendar size={11} /> Fecha</span>
                    </th>
                    <th className="text-right px-4 py-2.5 text-xs font-semibold text-gray-600">
                      <span className="flex items-center justify-end gap-1"><Hash size={11} /> Usuarios</span>
                    </th>
                    <th className="text-left px-4 py-2.5 text-xs font-semibold text-gray-600">Creado por</th>
                    <th className="px-4 py-2.5"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {snapshots.map(snap => (
                    <tr key={snap.id} className="hover:bg-gray-50 transition-colors">
                      <td className="px-4 py-3 font-medium text-gray-800">{snap.nombre}</td>
                      <td className="px-4 py-3 text-gray-500 text-xs">
                        {new Date(snap.fechaInstantanea).toLocaleString('es-CR', {
                          day: '2-digit', month: 'short', year: 'numeric',
                          hour: '2-digit', minute: '2-digit',
                        })}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <span className="inline-flex items-center justify-center bg-sky-100 text-sky-700 text-xs font-semibold px-2 py-0.5 rounded-full">
                          {snap.totalRegistros.toLocaleString()}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-gray-500 text-xs">{snap.creadoPor ?? '—'}</td>
                      <td className="px-4 py-3 text-right">
                        <button
                          onClick={() => descargarSnapshot(snap)}
                          disabled={descargandoId === snap.id}
                          className="flex items-center gap-1.5 text-xs text-blue-600 hover:text-blue-800 disabled:opacity-50 transition ml-auto"
                        >
                          {descargandoId === snap.id
                            ? <RefreshCw size={13} className="animate-spin" />
                            : <Download size={13} />}
                          Excel
                        </button>
                      </td>
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
