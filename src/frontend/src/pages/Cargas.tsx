import { useState, useRef, useEffect, useCallback } from 'react';
import { cargasApi, type CargaResultado, type SnapshotEntraIDDto, type MatrizPuestoDto, type LoteCargaDto } from '../api/cargas';
import api from '../api/client';
import { toast } from 'sonner';
import {
  Upload, Download, FileSpreadsheet, Users, ShieldCheck,
  CheckCircle2, XCircle, AlertTriangle, RefreshCw, X, LayoutGrid, Briefcase,
  MonitorSmartphone, Calendar, Hash, DatabaseZap, Search, ChevronLeft, ChevronRight, Eye,
  Building2, History, BadgeCheck, Clock, Zap
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
    descripcion: 'Importa o actualiza el padrón de empleados desde nómina (Evolution HR). La Cédula es la clave de cruce SAP ↔ Nómina ↔ Entra ID.',
    icon: <Users size={20} className="text-blue-600" />,
    columnas: ['NumeroEmpleado*', 'Cedula* (clave cruce)', 'Nombre*', 'ApellidoPaterno*', 'ApellidoMaterno',
               'CorreoCorporativo', 'FechaIngreso (YYYY-MM-DD)',
               'EstadoLaboral (ACTIVO/INACTIVO/BAJA_PROCESADA)',
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
// Visor Matriz de Puestos
// ─────────────────────────────────────────────────────────────────────────────
function MatrizVisor({ onRecargar }: { onRecargar?: number }) {
  const [items, setItems] = useState<MatrizPuestoDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [filtros, setFiltros] = useState({ usuario: '', puesto: '', rol: '', transaccion: '' });
  const [filtrosActivos, setFiltrosActivos] = useState({ usuario: '', puesto: '', rol: '', transaccion: '' });
  const PAGE_SIZE = 50;

  const cargar = useCallback(async (f = filtrosActivos, p = page) => {
    setLoading(true);
    try {
      const res = await cargasApi.getMatrizPuestos({ ...f, page: p, pageSize: PAGE_SIZE });
      setItems(res.items);
      setTotal(res.total);
    } catch {
      toast.error('Error al cargar la Matriz de Puestos');
    } finally {
      setLoading(false);
    }
  }, [filtrosActivos, page]);

  useEffect(() => { cargar(); }, [onRecargar]);

  const buscar = () => {
    setPage(1);
    setFiltrosActivos(filtros);
    cargar(filtros, 1);
  };

  const limpiar = () => {
    const vacio = { usuario: '', puesto: '', rol: '', transaccion: '' };
    setFiltros(vacio);
    setFiltrosActivos(vacio);
    setPage(1);
    cargar(vacio, 1);
  };

  const totalPaginas = Math.ceil(total / PAGE_SIZE);

  const irPagina = (p: number) => {
    setPage(p);
    cargar(filtrosActivos, p);
  };

  return (
    <div className="mt-8">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm font-semibold text-gray-700 flex items-center gap-2">
          <Eye size={15} className="text-violet-600" />
          Registros cargados
          {total > 0 && (
            <span className="bg-violet-100 text-violet-700 text-xs font-semibold px-2 py-0.5 rounded-full">
              {total.toLocaleString()}
            </span>
          )}
        </h2>
        <button onClick={() => cargar()} className="flex items-center gap-1 text-xs text-gray-500 hover:text-violet-600 transition">
          <RefreshCw size={12} className={loading ? 'animate-spin' : ''} /> Refrescar
        </button>
      </div>

      {/* Filtros */}
      <div className="grid grid-cols-4 gap-2 mb-3">
        {[
          { key: 'usuario', placeholder: 'Usuario / Nombre' },
          { key: 'puesto',  placeholder: 'Puesto' },
          { key: 'rol',     placeholder: 'Rol SAP' },
          { key: 'transaccion', placeholder: 'Transacción' },
        ].map(({ key, placeholder }) => (
          <input
            key={key}
            value={filtros[key as keyof typeof filtros]}
            onChange={e => setFiltros(f => ({ ...f, [key]: e.target.value }))}
            onKeyDown={e => e.key === 'Enter' && buscar()}
            placeholder={placeholder}
            className="border border-gray-200 rounded-lg px-3 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-violet-400"
          />
        ))}
      </div>
      <div className="flex gap-2 mb-4">
        <button onClick={buscar}
          className="flex items-center gap-1.5 bg-violet-600 text-white px-3 py-1.5 rounded-lg text-xs font-medium hover:bg-violet-700 transition">
          <Search size={12} /> Buscar
        </button>
        <button onClick={limpiar}
          className="text-xs text-gray-500 hover:text-gray-700 transition">
          Limpiar
        </button>
      </div>

      {/* Tabla */}
      {loading ? (
        <div className="flex items-center justify-center py-12 text-gray-400 text-sm gap-2">
          <RefreshCw size={16} className="animate-spin" /> Cargando...
        </div>
      ) : items.length === 0 ? (
        <div className="text-center py-12 text-gray-400 text-sm border border-dashed border-gray-200 rounded-xl">
          No hay registros. Carga un archivo para comenzar.
        </div>
      ) : (
        <>
          <div className="border border-gray-200 rounded-xl overflow-auto">
            <table className="w-full text-xs min-w-[900px]">
              <thead className="bg-gray-50 border-b border-gray-200 sticky top-0">
                <tr>
                  {['Usuario', 'Nombre', 'Puesto', 'Departamento', 'Rol', 'Transacción', 'Inicio Validez', 'Fin Validez', 'Último Ingreso'].map(h => (
                    <th key={h} className="text-left px-3 py-2 font-semibold text-gray-600 whitespace-nowrap">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {items.map((m, i) => (
                  <tr key={i} className="hover:bg-violet-50 transition-colors">
                    <td className="px-3 py-2 font-mono font-medium text-violet-700">{m.usuarioSAP}</td>
                    <td className="px-3 py-2 text-gray-700">{m.nombreCompleto ?? '—'}</td>
                    <td className="px-3 py-2 text-gray-600">{m.puesto ?? '—'}</td>
                    <td className="px-3 py-2 text-gray-500">{m.departamento ?? '—'}</td>
                    <td className="px-3 py-2">
                      <span className="bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded font-mono text-[11px]">{m.rol}</span>
                    </td>
                    <td className="px-3 py-2">
                      <span className="bg-emerald-100 text-emerald-700 px-1.5 py-0.5 rounded font-mono text-[11px]">{m.transaccion ?? '—'}</span>
                    </td>
                    <td className="px-3 py-2 text-gray-500">{m.inicioValidez ?? '—'}</td>
                    <td className="px-3 py-2 text-gray-500">{m.finValidez ?? '—'}</td>
                    <td className="px-3 py-2 text-gray-500">{m.ultimoIngreso ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Paginación */}
          {totalPaginas > 1 && (
            <div className="flex items-center justify-between mt-3">
              <p className="text-xs text-gray-500">
                Página {page} de {totalPaginas} · {total.toLocaleString()} registros
              </p>
              <div className="flex items-center gap-1">
                <button onClick={() => irPagina(page - 1)} disabled={page === 1}
                  className="p-1 rounded hover:bg-gray-100 disabled:opacity-30 transition">
                  <ChevronLeft size={14} />
                </button>
                {Array.from({ length: Math.min(5, totalPaginas) }, (_, i) => {
                  const p = Math.max(1, Math.min(page - 2, totalPaginas - 4)) + i;
                  return (
                    <button key={p} onClick={() => irPagina(p)}
                      className={`w-6 h-6 text-xs rounded transition ${p === page ? 'bg-violet-600 text-white' : 'hover:bg-gray-100 text-gray-600'}`}>
                      {p}
                    </button>
                  );
                })}
                <button onClick={() => irPagina(page + 1)} disabled={page === totalPaginas}
                  className="p-1 rounded hover:bg-gray-100 disabled:opacity-30 transition">
                  <ChevronRight size={14} />
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Historial de Lotes
// ─────────────────────────────────────────────────────────────────────────────
const TIPO_LABEL: Record<string, string> = {
  SAP_ROLES: 'SAP Roles',
  MATRIZ_PUESTOS: 'Matriz Puestos',
  EMPLEADOS: 'Empleados',
  CASOS_SESUITE: 'SE Suite',
  ENTRA_ID: 'Entra ID',
};
const TIPO_COLOR: Record<string, string> = {
  SAP_ROLES: 'bg-emerald-100 text-emerald-700',
  MATRIZ_PUESTOS: 'bg-violet-100 text-violet-700',
  EMPLEADOS: 'bg-blue-100 text-blue-700',
  CASOS_SESUITE: 'bg-amber-100 text-amber-700',
  ENTRA_ID: 'bg-sky-100 text-sky-700',
};

function LotesHistorial({ refrescar }: { refrescar?: number }) {
  const [lotes, setLotes] = useState<LoteCargaDto[]>([]);
  const [loading, setLoading] = useState(false);

  const cargar = useCallback(async () => {
    setLoading(true);
    try {
      const data = await cargasApi.getLotes({ limit: 100 });
      setLotes(data);
    } catch {
      toast.error('Error al cargar historial de lotes');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { cargar(); }, [refrescar]);

  const fmtFecha = (iso: string) =>
    new Date(iso).toLocaleString('es-CR', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });

  return (
    <div className="mt-10">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm font-semibold text-gray-700 flex items-center gap-2">
          <History size={15} className="text-gray-500" />
          Historial de Cargas (Lotes)
        </h2>
        <button onClick={cargar}
          className="flex items-center gap-1 text-xs text-gray-500 hover:text-blue-600 transition">
          <RefreshCw size={12} className={loading ? 'animate-spin' : ''} /> Refrescar
        </button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-10 text-gray-400 text-sm gap-2">
          <RefreshCw size={16} className="animate-spin" /> Cargando historial...
        </div>
      ) : lotes.length === 0 ? (
        <div className="text-center py-10 text-gray-400 text-sm border border-dashed border-gray-200 rounded-xl">
          No hay lotes de carga registrados todavía.
        </div>
      ) : (
        <div className="border border-gray-200 rounded-xl overflow-hidden">
          <table className="w-full text-xs min-w-[750px]">
            <thead className="bg-gray-50 border-b border-gray-200 sticky top-0">
              <tr>
                {['Tipo', 'Sociedad', 'Archivo', 'Fecha carga', 'Total', 'Insert.', 'Actual.', 'Errores', 'Estado'].map(h => (
                  <th key={h} className="text-left px-3 py-2 font-semibold text-gray-600 whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {lotes.map(l => (
                <tr key={l.id} className={`hover:bg-gray-50 transition-colors ${!l.esVigente ? 'opacity-50' : ''}`}>
                  <td className="px-3 py-2">
                    <span className={`px-2 py-0.5 rounded-full text-[11px] font-medium ${TIPO_COLOR[l.tipoCarga] ?? 'bg-gray-100 text-gray-700'}`}>
                      {TIPO_LABEL[l.tipoCarga] ?? l.tipoCarga}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-gray-700">
                    {l.sociedadNombre
                      ? <span title={l.sociedadCodigo ?? ''}>{l.sociedadNombre}</span>
                      : <span className="text-gray-400">—</span>}
                  </td>
                  <td className="px-3 py-2 text-gray-500 max-w-[160px] truncate" title={l.nombreArchivo ?? ''}>
                    {l.nombreArchivo ?? '—'}
                  </td>
                  <td className="px-3 py-2 text-gray-600 whitespace-nowrap">
                    <span className="flex items-center gap-1">
                      <Clock size={11} className="text-gray-400" />
                      {fmtFecha(l.fechaCarga)}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-gray-700 font-mono">{l.totalRegistros.toLocaleString()}</td>
                  <td className="px-3 py-2 text-green-700 font-mono">{l.insertados.toLocaleString()}</td>
                  <td className="px-3 py-2 text-blue-700 font-mono">{l.actualizados.toLocaleString()}</td>
                  <td className="px-3 py-2 font-mono">
                    <span className={l.errores > 0 ? 'text-red-600' : 'text-gray-400'}>{l.errores}</span>
                  </td>
                  <td className="px-3 py-2">
                    {l.esVigente
                      ? <span className="flex items-center gap-1 text-green-700 font-medium">
                          <BadgeCheck size={13} /> Vigente
                        </span>
                      : <span className="text-gray-400">Anterior</span>}
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

// ─────────────────────────────────────────────────────────────────────────────
// Page
// ─────────────────────────────────────────────────────────────────────────────
const SISTEMAS = ['SAP', 'EVOLUTION', 'SE_SUITE', 'AD', 'OTRO'];

interface SociedadOpcion { id: number; codigo: string; nombre: string; }

export function Cargas() {
  const [tipo, setTipo] = useState<TipoCarga>('empleados');
  const [sistema, setSistema] = useState('SAP');
  const [state, setState] = useState<CargaState>({ file: null, resultado: null, loading: false, dragging: false });
  const [nombreSnapshot, setNombreSnapshot] = useState('');
  const [snapshots, setSnapshots] = useState<SnapshotEntraIDDto[]>([]);
  const [loadingSnapshots, setLoadingSnapshots] = useState(false);
  const [descargandoId, setDescargandoId] = useState<string | null>(null);
  const [ultimoSnapshot, setUltimoSnapshot] = useState<{ id: string; nombre: string; origen?: string } | null>(null);
  const [syncLoading, setSyncLoading] = useState(false);
  const [recargarVisor, setRecargarVisor] = useState(0);
  const [recargarLotes, setRecargarLotes] = useState(0);
  const [sociedadCodigo, setSociedadCodigo] = useState('');
  const [sociedades, setSociedades] = useState<SociedadOpcion[]>([]);
  const cfg = TIPO_CONFIG[tipo];

  // Cargar lista de sociedades activas al montar
  useEffect(() => {
    api.get('/sociedades').then(r => setSociedades(r.data)).catch(() => {});
  }, []);

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

  const handleSyncDirecto = async () => {
    setSyncLoading(true);
    setState(s => ({ ...s, resultado: null }));
    setUltimoSnapshot(null);
    try {
      const res = await cargasApi.syncEntraIDDirecto(nombreSnapshot || undefined);
      const resultado: CargaResultado = {
        totalRegistros: res.totalRegistros,
        insertados: res.totalRegistros - res.errores,
        actualizados: 0,
        errores: res.errores,
        detalleErrores: res.detalleErrores,
      };
      setState(s => ({ ...s, resultado }));
      setUltimoSnapshot({ id: res.snapshotId, nombre: res.nombre, origen: res.origen });
      if (res.errores === 0)
        toast.success(`Snapshot "${res.nombre}" sincronizado: ${res.totalRegistros} usuarios`);
      else
        toast.warning(`Sync completado con ${res.errores} error(es)`);
      cargasApi.getSnapshotsEntraID().then(setSnapshots).catch(() => {});
      setNombreSnapshot('');
      setRecargarLotes(v => v + 1);
    } catch (err: unknown) {
      const apiMsg = (err as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      toast.error(apiMsg ?? 'Error al sincronizar con Microsoft Graph. Verifica la Managed Identity.');
    } finally {
      setSyncLoading(false);
    }
  };

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

      const cod = sociedadCodigo || undefined;
      const resultado =
        tipo === 'empleados'     ? await cargasApi.cargarEmpleados(archivo, cod) :
        tipo === 'sapRoles'      ? await cargasApi.cargarRolesSAP(archivo, sistema, cod) :
        tipo === 'matrizPuestos' ? await cargasApi.cargarMatrizPuestos(archivo, cod) :
                                   await cargasApi.cargarCasosSeSuite(archivo, cod);
      setState(s => ({ ...s, resultado, file: null }));
      if (resultado.errores === 0) {
        toast.success(`Carga completada: ${resultado.insertados} nuevos, ${resultado.actualizados} actualizados`);
        if (tipo === 'matrizPuestos') setRecargarVisor(v => v + 1);
        setRecargarLotes(v => v + 1);
      } else {
        toast.warning(`Carga con errores: ${resultado.errores} filas no procesadas`);
        setRecargarLotes(v => v + 1);
      }
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

      {/* Selector de Sociedad (para cargas que no sean EntraID) */}
      {tipo !== 'entraID' && (
        <div className="mb-4 flex items-center gap-3">
          <Building2 size={15} className="text-gray-500 flex-shrink-0" />
          <label className="text-sm font-medium text-gray-700 whitespace-nowrap">Sociedad SAP:</label>
          <select
            value={sociedadCodigo}
            onChange={e => setSociedadCodigo(e.target.value)}
            className="flex-1 border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">— Sin especificar —</option>
            {sociedades.map(s => (
              <option key={s.codigo} value={s.codigo}>{s.codigo} · {s.nombre}</option>
            ))}
          </select>
        </div>
      )}

      {/* Entra ID: sincronización directa via Graph (opción primaria) */}
      {tipo === 'entraID' && (
        <div className="mb-4 bg-sky-50 border border-sky-200 rounded-xl p-4 space-y-3">
          <div className="flex items-center gap-2 text-sky-700 font-semibold text-sm">
            <Zap size={16} />
            Sincronización directa — Microsoft Graph
          </div>
          <p className="text-xs text-gray-500 leading-relaxed">
            Conecta directo al tenant Azure AD vía Managed Identity. No requiere exportar Excel.
          </p>
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
          <button
            onClick={handleSyncDirecto}
            disabled={syncLoading || state.loading}
            className="w-full flex items-center justify-center gap-2 bg-sky-600 text-white px-5 py-2.5 rounded-lg text-sm font-semibold hover:bg-sky-700 disabled:opacity-50 transition"
          >
            {syncLoading
              ? <><RefreshCw size={15} className="animate-spin" /> Sincronizando con Azure AD...</>
              : <><Zap size={15} /> Sincronizar Entra ID</>}
          </button>
          <div className="flex items-center gap-1.5 text-[11px] text-sky-600">
            <CheckCircle2 size={11} />Requiere permiso <span className="font-mono bg-sky-100 px-1 rounded">User.Read.All</span> en la Managed Identity
          </div>
        </div>
      )}

      {/* Entra ID: divisor antes del fallback Excel */}
      {tipo === 'entraID' && (
        <div className="relative flex items-center mb-4">
          <div className="flex-grow border-t border-gray-200" />
          <span className="mx-3 text-xs text-gray-400 whitespace-nowrap">o importa desde Excel</span>
          <div className="flex-grow border-t border-gray-200" />
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
            <button onClick={handleCarga} disabled={state.loading || syncLoading}
              className="flex items-center gap-2 bg-slate-600 text-white px-5 py-2.5 rounded-lg text-sm font-medium hover:bg-slate-700 disabled:opacity-50 transition">
              {state.loading ? <RefreshCw size={15} className="animate-spin" /> : <DatabaseZap size={15} />}
              {state.loading ? 'Importando...' : 'Importar desde Excel'}
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
                <span className="font-semibold">"{ultimoSnapshot.nombre}"</span> guardado correctamente.{' '}
                {ultimoSnapshot.origen === 'GRAPH_DIRECT'
                  ? <span className="inline-flex items-center gap-1 text-xs bg-sky-200 text-sky-800 px-1.5 py-0.5 rounded font-mono"><Zap size={10} />GRAPH_DIRECT</span>
                  : <span className="inline-flex items-center gap-1 text-xs bg-slate-200 text-slate-700 px-1.5 py-0.5 rounded font-mono"><DatabaseZap size={10} />MANUAL_EXCEL</span>}
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

      {/* Visor Matriz de Puestos */}
      {tipo === 'matrizPuestos' && (
        <MatrizVisor onRecargar={recargarVisor} />
      )}

      {/* Historial de Lotes de Carga (siempre visible) */}
      <LotesHistorial refrescar={recargarLotes} />

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
                    <th className="text-left px-4 py-2.5 text-xs font-semibold text-gray-600">Origen</th>
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
                      <td className="px-4 py-3">
                        {snap.origen === 'GRAPH_DIRECT'
                          ? <span className="inline-flex items-center gap-1 text-[11px] bg-sky-100 text-sky-700 px-1.5 py-0.5 rounded font-mono"><Zap size={9} />Graph</span>
                          : <span className="inline-flex items-center gap-1 text-[11px] bg-slate-100 text-slate-600 px-1.5 py-0.5 rounded font-mono"><DatabaseZap size={9} />Excel</span>}
                      </td>
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
