import { useState, useRef, useEffect, useCallback } from 'react';
import {
  cargasApi, type CargaResultado, type SnapshotEntraIDDto, type LoteCargaDto,
  type EmpleadoVisorDto, type UsuarioSAPVisorDto, type CasoVisorDto, type RegistroEntraIDVisorDto,
} from '../api/cargas';
import api from '../api/client';
import { toast } from 'sonner';
import {
  Upload, Download, FileSpreadsheet, Users, ShieldCheck, LayoutGrid, Briefcase,
  MonitorSmartphone, CheckCircle2, XCircle, AlertTriangle, RefreshCw, X,
  Building2, Zap, DatabaseZap, Search, ChevronLeft, ChevronRight,
  BadgeCheck, Clock, Trash2, AlertOctagon, ArrowUpFromLine, Filter,
} from 'lucide-react';
type TipoCarga = 'empleados' | 'sapRoles' | 'matrizPuestos' | 'casosSeSuite' | 'entraID';

const PAGE_SIZE = 50;

// ─── Configuración de cada tipo ───────────────────────────────────────────────
const TIPO_CONFIG: Record<TipoCarga, {
  label: string; labelCorto: string; descripcion: string;
  icon: React.ReactNode; color: string; bgColor: string; borderColor: string;
  columnas: string[];
}> = {
  empleados: {
    label: 'Empleados Maestro', labelCorto: 'Empleados',
    descripcion: 'Padrón de empleados desde nómina (Evolution HR). La Cédula es la clave de cruce.',
    icon: <Users size={18} />, color: 'text-blue-600', bgColor: 'bg-blue-50', borderColor: 'border-blue-200',
    columnas: ['NumeroEmpleado*','Cedula* (clave cruce)','Nombre*','ApellidoPaterno*','ApellidoMaterno',
               'CorreoCorporativo','FechaIngreso (YYYY-MM-DD)','EstadoLaboral (ACTIVO/INACTIVO/BAJA_PROCESADA)',
               'DepartamentoCodigo','PuestoCodigo'],
  },
  sapRoles: {
    label: 'SAP — Usuarios y Roles', labelCorto: 'SAP Roles',
    descripcion: 'Exporta V_SAP_USR_RECERTIFICAION. Una fila por usuario+rol+transacción.',
    icon: <ShieldCheck size={18} />, color: 'text-emerald-600', bgColor: 'bg-emerald-50', borderColor: 'border-emerald-200',
    columnas: ['ID (Cédula)','USUARIO*','NOMBRE_COMPLETO','SOCIEDAD','DEPARTAMENTO','PUESTO',
               'EMAIL','ROL*','INICIO_VALIDEZ','FIN_VALIDEZ','TRANSACCION*','ULTIMO_INGRESO'],
  },
  matrizPuestos: {
    label: 'Matriz de Puestos (Contraloría)', labelCorto: 'Matriz',
    descripcion: 'Matriz oficial de roles por puesto aprobada por Contraloría.',
    icon: <LayoutGrid size={18} />, color: 'text-violet-600', bgColor: 'bg-violet-50', borderColor: 'border-violet-200',
    columnas: ['ID (Cédula)','USUARIO*','NOMBRE_COMPLETO','SOCIEDAD','DEPARTAMENTO','PUESTO*',
               'EMAIL','ROL*','INICIO_VALIDEZ','FIN_VALIDEZ','TRANSACCION*','ULTIMO_INGRESO','FECHA_REVISION_CONTRALORIA'],
  },
  casosSeSuite: {
    label: 'Casos SE Suite', labelCorto: 'Casos SE',
    descripcion: 'Casos aprobados que justifican accesos fuera de la Matriz de Puestos.',
    icon: <Briefcase size={18} />, color: 'text-amber-600', bgColor: 'bg-amber-50', borderColor: 'border-amber-200',
    columnas: ['NUMERO_CASO*','TITULO','USUARIO_SAP*','CEDULA','ROL_JUSTIFICADO*',
               'TRANSACCIONES','FECHA_APROBACION','FECHA_VENCIMIENTO','ESTADO','APROBADOR'],
  },
  entraID: {
    label: 'Entra ID (Azure AD)', labelCorto: 'Entra ID',
    descripcion: 'Snapshot del directorio corporativo Azure AD por cédula.',
    icon: <MonitorSmartphone size={18} />, color: 'text-sky-600', bgColor: 'bg-sky-50', borderColor: 'border-sky-200',
    columnas: ['EmployeeId* (Cédula)','ObjectId','DisplayName*','UserPrincipalName*','Email',
               'Department','JobTitle','AccountEnabled','Manager','OfficeLocation','CreatedDateTime','LastSignInDateTime'],
  },
};

const ESTADO_EMPLEADO_BADGE: Record<string, string> = {
  ACTIVO: 'bg-green-100 text-green-700', INACTIVO: 'bg-gray-100 text-gray-500',
  BAJA_PROCESADA: 'bg-red-100 text-red-700',
};
const ESTADO_SAP_BADGE: Record<string, string> = {
  ACTIVO: 'bg-green-100 text-green-700', INACTIVO: 'bg-gray-100 text-gray-500', BLOQUEADO: 'bg-red-100 text-red-700',
};
const ESTADO_CASO_BADGE: Record<string, string> = {
  APROBADO: 'bg-green-100 text-green-700', VENCIDO: 'bg-red-100 text-red-700', ANULADO: 'bg-gray-100 text-gray-500',
};
const TIPO_COLOR: Record<string, string> = {
  SAP_ROLES: 'bg-emerald-100 text-emerald-700', MATRIZ_PUESTOS: 'bg-violet-100 text-violet-700',
  EMPLEADOS: 'bg-blue-100 text-blue-700', CASOS_SESUITE: 'bg-amber-100 text-amber-700',
  ENTRA_ID: 'bg-sky-100 text-sky-700',
};
const TIPO_LABEL: Record<string, string> = {
  SAP_ROLES: 'SAP Roles', MATRIZ_PUESTOS: 'Matriz', EMPLEADOS: 'Empleados',
  CASOS_SESUITE: 'SE Suite', ENTRA_ID: 'Entra ID',
};

// ─── Helpers ──────────────────────────────────────────────────────────────────
function fmtFecha(iso: string) {
  return new Date(iso).toLocaleString('es-CR', {
    day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

function Badge({ text, className }: { text: string; className: string }) {
  return <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium ${className}`}>{text}</span>;
}

function Pager({ page, total, pageSize, onPage }: { page: number; total: number; pageSize: number; onPage: (p: number) => void }) {
  const totalPages = Math.ceil(total / pageSize);
  if (totalPages <= 1) return null;
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);
  const pages = Array.from({ length: Math.min(5, totalPages) }, (_, i) =>
    Math.max(1, Math.min(page - 2, totalPages - 4)) + i);
  return (
    <div className="flex items-center justify-between px-4 py-3 border-t border-gray-100 bg-gray-50/50">
      <span className="text-xs text-gray-500">{from.toLocaleString()}–{to.toLocaleString()} de <strong>{total.toLocaleString()}</strong></span>
      <div className="flex items-center gap-1">
        <button onClick={() => onPage(page - 1)} disabled={page === 1}
          className="p-1.5 rounded-lg hover:bg-gray-200 disabled:opacity-30 transition"><ChevronLeft size={13} /></button>
        {pages.map(p => (
          <button key={p} onClick={() => onPage(p)}
            className={`w-7 h-7 text-xs rounded-lg transition font-medium ${p === page ? 'bg-blue-600 text-white shadow-sm' : 'hover:bg-gray-200 text-gray-600'}`}>{p}</button>
        ))}
        <button onClick={() => onPage(page + 1)} disabled={page === totalPages}
          className="p-1.5 rounded-lg hover:bg-gray-200 disabled:opacity-30 transition"><ChevronRight size={13} /></button>
      </div>
    </div>
  );
}

// ─── Drop Zone ────────────────────────────────────────────────────────────────
function DropZone({ onFile, file, onClear, loading }: { onFile: (f: File) => void; file: File | null; onClear: () => void; loading: boolean }) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  return (
    <div
      onDrop={e => { e.preventDefault(); setDragging(false); const f = e.dataTransfer.files[0]; if (f) onFile(f); }}
      onDragOver={e => { e.preventDefault(); setDragging(true); }}
      onDragLeave={() => setDragging(false)}
      onClick={() => !file && inputRef.current?.click()}
      className={`relative border-2 border-dashed rounded-xl p-6 text-center transition-all cursor-pointer
        ${dragging ? 'border-blue-400 bg-blue-50' : file ? 'border-green-300 bg-green-50 cursor-default' : 'border-gray-200 hover:border-blue-400 hover:bg-blue-50/30'}`}
    >
      <input ref={inputRef} type="file" className="hidden" accept=".xlsx,.xls,.csv"
        onChange={e => { const f = e.target.files?.[0]; if (f) onFile(f); e.target.value = ''; }} />
      {file ? (
        <div className="flex items-center justify-center gap-3">
          <FileSpreadsheet size={24} className="text-green-600" />
          <div className="text-left">
            <p className="font-medium text-green-700 text-sm">{file.name}</p>
            <p className="text-xs text-green-600">{(file.size / 1024).toFixed(1)} KB</p>
          </div>
          <button onClick={e => { e.stopPropagation(); onClear(); }} className="ml-2 text-gray-400 hover:text-red-500 transition"><X size={15} /></button>
        </div>
      ) : (
        <>
          <Upload size={28} className="mx-auto text-gray-400 mb-2" />
          <p className="font-medium text-gray-700 text-sm">Arrastra el archivo aquí</p>
          <p className="text-xs text-gray-500 mt-1">o <span className="text-blue-600 underline">haz clic para seleccionar</span></p>
          <p className="text-[11px] text-gray-400 mt-1">Excel (.xlsx) o CSV · máx 50 MB</p>
        </>
      )}
      {loading && (
        <div className="absolute inset-0 bg-white/80 rounded-xl flex items-center justify-center gap-2">
          <RefreshCw size={20} className="animate-spin text-blue-600" />
          <span className="text-sm text-blue-600 font-medium">Procesando...</span>
        </div>
      )}
    </div>
  );
}

// ─── Resultado upload ─────────────────────────────────────────────────────────
function ResultadoPanel({ resultado, onClose }: { resultado: CargaResultado; onClose: () => void }) {
  const ok = resultado.errores === 0;
  return (
    <div className={`rounded-xl border p-4 ${ok ? 'bg-green-50 border-green-200' : 'bg-yellow-50 border-yellow-200'}`}>
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-2">
          {ok ? <CheckCircle2 size={18} className="text-green-600" /> : <AlertTriangle size={18} className="text-yellow-600" />}
          <span className="font-semibold text-sm text-gray-800">
            {ok ? 'Carga completada sin errores' : `Carga con ${resultado.errores} error(es)`}
          </span>
        </div>
        <button onClick={onClose} className="text-gray-400 hover:text-gray-600"><X size={15} /></button>
      </div>
      <div className="grid grid-cols-4 gap-2 mb-3">
        {[
          { l: 'Total', v: resultado.totalRegistros, c: 'text-gray-700' },
          { l: 'Insertados', v: resultado.insertados, c: 'text-green-700' },
          { l: 'Actualizados', v: resultado.actualizados, c: 'text-blue-700' },
          { l: 'Errores', v: resultado.errores, c: resultado.errores > 0 ? 'text-red-700' : 'text-gray-400' },
        ].map(s => (
          <div key={s.l} className="bg-white rounded-lg px-3 py-2 text-center shadow-sm">
            <p className="text-[11px] text-gray-500">{s.l}</p>
            <p className={`text-lg font-bold mt-0.5 ${s.c}`}>{s.v}</p>
          </div>
        ))}
      </div>
      {resultado.detalleErrores.length > 0 && (
        <div className="bg-white rounded-lg border border-red-200 max-h-36 overflow-y-auto">
          {resultado.detalleErrores.map((e, i) => (
            <div key={i} className={`px-3 py-1.5 text-xs text-red-700 ${i > 0 ? 'border-t border-red-100' : ''}`}>
              <XCircle size={11} className="inline mr-1" />{e}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// ─── Visor Empleados ──────────────────────────────────────────────────────────
function VisorEmpleados({ refreshKey }: { refreshKey: number }) {
  const [data, setData] = useState<EmpleadoVisorDto[]>([]);
  const [total, setTotal] = useState(0); const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [q, setQ] = useState(''); const [estado, setEstado] = useState('');
  const [activeQ, setActiveQ] = useState(''); const [activeEstado, setActiveEstado] = useState('');

  const load = useCallback(async (aq = activeQ, ae = activeEstado, p = page) => {
    setLoading(true);
    try {
      const r = await cargasApi.getEmpleados({ q: aq || undefined, estado: ae || undefined, page: p, pageSize: PAGE_SIZE });
      setData(r.items); setTotal(r.total);
    } catch { toast.error('Error al cargar empleados'); } finally { setLoading(false); }
  }, [activeQ, activeEstado, page]);

  useEffect(() => { load(); }, [refreshKey]);

  const buscar = () => { setActiveQ(q); setActiveEstado(estado); setPage(1); load(q, estado, 1); };
  const limpiar = () => { setQ(''); setEstado(''); setActiveQ(''); setActiveEstado(''); setPage(1); load('', '', 1); };
  const irPagina = (p: number) => { setPage(p); load(activeQ, activeEstado, p); };

  return (
    <div className="flex flex-col gap-4">
      <div className="flex gap-2 flex-wrap">
        <div className="relative flex-1 min-w-[200px]">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && buscar()}
            placeholder="Nombre, cédula o N° empleado…"
            className="w-full pl-8 pr-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-400" />
        </div>
        <select value={estado} onChange={e => setEstado(e.target.value)}
          className="border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400 bg-white">
          <option value="">Todos los estados</option>
          <option>ACTIVO</option><option>INACTIVO</option><option>BAJA_PROCESADA</option>
        </select>
        <button onClick={buscar} className="flex items-center gap-1.5 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition">
          <Search size={13} />Buscar</button>
        {(activeQ || activeEstado) && <button onClick={limpiar} className="text-sm text-gray-500 hover:text-gray-700 px-2">✕ Limpiar</button>}
      </div>
      <VisorTable loading={loading} empty={total === 0}
        headers={['N° Emp.','Cédula','Nombre','Correo','Estado','F. Ingreso','F. Baja']}>
        {data.map((e, i) => (
          <tr key={i} className="hover:bg-blue-50/40 transition-colors">
            <td className="px-3 py-2.5 font-mono text-[12px] text-gray-500">{e.numeroEmpleado}</td>
            <td className="px-3 py-2.5 font-mono text-[12px] font-medium text-gray-700">{e.cedula ?? '—'}</td>
            <td className="px-3 py-2.5 font-medium text-gray-800 text-sm">{e.nombreCompleto}</td>
            <td className="px-3 py-2.5 text-gray-500 text-xs">{e.correoCorporativo ?? '—'}</td>
            <td className="px-3 py-2.5">
              <Badge text={e.estadoLaboral} className={ESTADO_EMPLEADO_BADGE[e.estadoLaboral] ?? 'bg-gray-100 text-gray-600'} />
            </td>
            <td className="px-3 py-2.5 text-gray-500 text-xs whitespace-nowrap">{e.fechaIngreso ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-500 text-xs whitespace-nowrap">{e.fechaBaja ?? '—'}</td>
          </tr>
        ))}
      </VisorTable>
      <Pager page={page} total={total} pageSize={PAGE_SIZE} onPage={irPagina} />
    </div>
  );
}

// ─── Visor SAP Roles ──────────────────────────────────────────────────────────
function VisorSAPRoles({ refreshKey }: { refreshKey: number }) {
  const [data, setData] = useState<UsuarioSAPVisorDto[]>([]);
  const [total, setTotal] = useState(0); const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [q, setQ] = useState(''); const [estado, setEstado] = useState('');
  const [activeQ, setActiveQ] = useState(''); const [activeEstado, setActiveEstado] = useState('');

  const load = useCallback(async (aq = activeQ, ae = activeEstado, p = page) => {
    setLoading(true);
    try {
      const r = await cargasApi.getUsuariosSAP({ q: aq || undefined, estado: ae || undefined, page: p, pageSize: PAGE_SIZE });
      setData(r.items); setTotal(r.total);
    } catch { toast.error('Error al cargar usuarios SAP'); } finally { setLoading(false); }
  }, [activeQ, activeEstado, page]);

  useEffect(() => { load(); }, [refreshKey]);

  const buscar = () => { setActiveQ(q); setActiveEstado(estado); setPage(1); load(q, estado, 1); };
  const limpiar = () => { setQ(''); setEstado(''); setActiveQ(''); setActiveEstado(''); setPage(1); load('', '', 1); };
  const irPagina = (p: number) => { setPage(p); load(activeQ, activeEstado, p); };

  return (
    <div className="flex flex-col gap-4">
      <div className="flex gap-2 flex-wrap">
        <div className="relative flex-1 min-w-[200px]">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && buscar()}
            placeholder="Usuario, cédula o nombre…"
            className="w-full pl-8 pr-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-emerald-400" />
        </div>
        <select value={estado} onChange={e => setEstado(e.target.value)}
          className="border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none bg-white">
          <option value="">Todos los estados</option>
          <option>ACTIVO</option><option>INACTIVO</option><option>BLOQUEADO</option>
        </select>
        <button onClick={buscar} className="flex items-center gap-1.5 bg-emerald-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-emerald-700 transition">
          <Search size={13} />Buscar</button>
        {(activeQ || activeEstado) && <button onClick={limpiar} className="text-sm text-gray-500 hover:text-gray-700 px-2">✕ Limpiar</button>}
      </div>
      <VisorTable loading={loading} empty={total === 0}
        headers={['Usuario SAP','Cédula','Nombre','Sociedad','Departamento','Puesto','Estado','Último Acceso']}>
        {data.map((u, i) => (
          <tr key={i} className="hover:bg-emerald-50/40 transition-colors">
            <td className="px-3 py-2.5 font-mono text-[12px] font-bold text-emerald-700">{u.nombreUsuario}</td>
            <td className="px-3 py-2.5 font-mono text-[12px] text-gray-600">{u.cedula ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-800 text-sm">{u.nombreCompleto ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-500 text-xs">{u.sociedad ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-500 text-xs">{u.departamento ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-500 text-xs">{u.puesto ?? '—'}</td>
            <td className="px-3 py-2.5">
              <Badge text={u.estado} className={ESTADO_SAP_BADGE[u.estado] ?? 'bg-gray-100 text-gray-600'} />
            </td>
            <td className="px-3 py-2.5 text-gray-400 text-xs whitespace-nowrap">{u.ultimoAcceso ?? '—'}</td>
          </tr>
        ))}
      </VisorTable>
      <Pager page={page} total={total} pageSize={PAGE_SIZE} onPage={irPagina} />
    </div>
  );
}

// ─── Visor Matriz de Puestos ──────────────────────────────────────────────────
function VisorMatriz({ refreshKey }: { refreshKey: number }) {
  const [items, setItems] = useState<import('../api/cargas').MatrizPuestoDto[]>([]);
  const [total, setTotal] = useState(0); const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [filtros, setFiltros] = useState({ usuario: '', puesto: '', rol: '', transaccion: '' });
  const [activos, setActivos] = useState({ usuario: '', puesto: '', rol: '', transaccion: '' });

  const load = useCallback(async (f = activos, p = page) => {
    setLoading(true);
    try {
      const r = await cargasApi.getMatrizPuestos({ ...f, page: p, pageSize: PAGE_SIZE });
      setItems(r.items); setTotal(r.total);
    } catch { toast.error('Error al cargar Matriz de Puestos'); } finally { setLoading(false); }
  }, [activos, page]);

  useEffect(() => { load(); }, [refreshKey]);

  const buscar = () => { setActivos(filtros); setPage(1); load(filtros, 1); };
  const limpiar = () => { const v = { usuario: '', puesto: '', rol: '', transaccion: '' }; setFiltros(v); setActivos(v); setPage(1); load(v, 1); };
  const irPagina = (p: number) => { setPage(p); load(activos, p); };
  const hayFiltros = Object.values(activos).some(Boolean);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex gap-2 flex-wrap">
        {[{ key: 'usuario', ph: 'Usuario / Nombre', w: 'flex-1 min-w-[150px]' },
          { key: 'puesto',  ph: 'Puesto', w: 'w-36' },
          { key: 'rol',     ph: 'Rol SAP', w: 'w-40' },
          { key: 'transaccion', ph: 'Transacción', w: 'w-36' }].map(({ key, ph, w }) => (
          <div key={key} className={`relative ${w}`}>
            <Search size={13} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400" />
            <input value={filtros[key as keyof typeof filtros]}
              onChange={e => setFiltros(f => ({ ...f, [key]: e.target.value }))}
              onKeyDown={e => e.key === 'Enter' && buscar()}
              placeholder={ph}
              className="w-full pl-7 pr-2 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-violet-400" />
          </div>
        ))}
        <button onClick={buscar} className="flex items-center gap-1.5 bg-violet-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-violet-700 transition">
          <Search size={13} />Buscar</button>
        {hayFiltros && <button onClick={limpiar} className="text-sm text-gray-500 hover:text-gray-700 px-2">✕ Limpiar</button>}
      </div>
      <VisorTable loading={loading} empty={total === 0}
        headers={['Usuario','Nombre','Puesto','Depto','Rol','Transacción','Inicio','Fin','Último Ingreso']}>
        {items.map((m, i) => (
          <tr key={i} className="hover:bg-violet-50/40 transition-colors">
            <td className="px-3 py-2.5 font-mono text-[12px] font-bold text-violet-700">{m.usuarioSAP}</td>
            <td className="px-3 py-2.5 text-gray-800 text-sm">{m.nombreCompleto ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-600 text-xs">{m.puesto ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-500 text-xs">{m.departamento ?? '—'}</td>
            <td className="px-3 py-2.5"><span className="bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded font-mono text-[11px]">{m.rol}</span></td>
            <td className="px-3 py-2.5"><span className="bg-emerald-100 text-emerald-700 px-1.5 py-0.5 rounded font-mono text-[11px]">{m.transaccion ?? '—'}</span></td>
            <td className="px-3 py-2.5 text-gray-400 text-xs whitespace-nowrap">{m.inicioValidez ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-400 text-xs whitespace-nowrap">{m.finValidez ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-400 text-xs whitespace-nowrap">{m.ultimoIngreso ?? '—'}</td>
          </tr>
        ))}
      </VisorTable>
      <Pager page={page} total={total} pageSize={PAGE_SIZE} onPage={irPagina} />
    </div>
  );
}

// ─── Visor Casos SE Suite ─────────────────────────────────────────────────────
function VisorCasosSE({ refreshKey }: { refreshKey: number }) {
  const [data, setData] = useState<CasoVisorDto[]>([]);
  const [total, setTotal] = useState(0); const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [q, setQ] = useState(''); const [estado, setEstado] = useState('');
  const [activeQ, setActiveQ] = useState(''); const [activeEstado, setActiveEstado] = useState('');
  const hoy = new Date().toISOString().split('T')[0];

  const load = useCallback(async (aq = activeQ, ae = activeEstado, p = page) => {
    setLoading(true);
    try {
      const r = await cargasApi.getCasosSESuite({ q: aq || undefined, estado: ae || undefined, page: p, pageSize: PAGE_SIZE });
      setData(r.items); setTotal(r.total);
    } catch { toast.error('Error al cargar casos SE Suite'); } finally { setLoading(false); }
  }, [activeQ, activeEstado, page]);

  useEffect(() => { load(); }, [refreshKey]);

  const buscar = () => { setActiveQ(q); setActiveEstado(estado); setPage(1); load(q, estado, 1); };
  const limpiar = () => { setQ(''); setEstado(''); setActiveQ(''); setActiveEstado(''); setPage(1); load('', '', 1); };
  const irPagina = (p: number) => { setPage(p); load(activeQ, activeEstado, p); };

  const isVencido = (fecha?: string) => fecha && fecha < hoy;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex gap-2 flex-wrap">
        <div className="relative flex-1 min-w-[200px]">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && buscar()}
            placeholder="Caso, usuario SAP, cédula o rol…"
            className="w-full pl-8 pr-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-400" />
        </div>
        <select value={estado} onChange={e => setEstado(e.target.value)}
          className="border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none bg-white">
          <option value="">Todos los estados</option>
          <option>APROBADO</option><option>VENCIDO</option><option>ANULADO</option>
        </select>
        <button onClick={buscar} className="flex items-center gap-1.5 bg-amber-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-amber-700 transition">
          <Search size={13} />Buscar</button>
        {(activeQ || activeEstado) && <button onClick={limpiar} className="text-sm text-gray-500 hover:text-gray-700 px-2">✕ Limpiar</button>}
      </div>
      <VisorTable loading={loading} empty={total === 0}
        headers={['Caso','Título','Usuario SAP','Cédula','Rol Justificado','F. Aprobación','F. Vencimiento','Estado','Aprobador']}>
        {data.map((c, i) => (
          <tr key={i} className={`hover:bg-amber-50/40 transition-colors ${isVencido(c.fechaVencimiento) && c.estadoCaso === 'APROBADO' ? 'bg-red-50/30' : ''}`}>
            <td className="px-3 py-2.5 font-mono text-[12px] font-bold text-amber-700">{c.numeroCaso}</td>
            <td className="px-3 py-2.5 text-gray-700 text-xs max-w-[180px] truncate" title={c.titulo ?? ''}>{c.titulo ?? '—'}</td>
            <td className="px-3 py-2.5 font-mono text-[12px] text-gray-600">{c.usuarioSAP ?? '—'}</td>
            <td className="px-3 py-2.5 font-mono text-[12px] text-gray-500">{c.cedula ?? '—'}</td>
            <td className="px-3 py-2.5"><span className="bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded font-mono text-[11px]">{c.rolJustificado ?? '—'}</span></td>
            <td className="px-3 py-2.5 text-gray-500 text-xs whitespace-nowrap">{c.fechaAprobacion ?? '—'}</td>
            <td className="px-3 py-2.5 text-xs whitespace-nowrap">
              <span className={isVencido(c.fechaVencimiento) && c.estadoCaso === 'APROBADO' ? 'text-red-600 font-semibold' : 'text-gray-500'}>
                {c.fechaVencimiento ?? '—'}
              </span>
            </td>
            <td className="px-3 py-2.5">
              <Badge text={c.estadoCaso} className={ESTADO_CASO_BADGE[c.estadoCaso] ?? 'bg-gray-100 text-gray-600'} />
            </td>
            <td className="px-3 py-2.5 text-gray-400 text-xs">{c.aprobador ?? '—'}</td>
          </tr>
        ))}
      </VisorTable>
      <Pager page={page} total={total} pageSize={PAGE_SIZE} onPage={irPagina} />
    </div>
  );
}

// ─── Visor Entra ID ───────────────────────────────────────────────────────────
function VisorEntraID({ snapshots, refreshKey }: { snapshots: SnapshotEntraIDDto[]; refreshKey: number }) {
  const [snapshotId, setSnapshotId] = useState<string>('');
  const [data, setData] = useState<RegistroEntraIDVisorDto[]>([]);
  const [total, setTotal] = useState(0); const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [q, setQ] = useState(''); const [accountEnabled, setAccountEnabled] = useState('');
  const [activeQ, setActiveQ] = useState(''); const [activeEnabled, setActiveEnabled] = useState('');

  useEffect(() => {
    if (snapshots.length > 0 && !snapshotId) setSnapshotId(snapshots[0].id);
  }, [snapshots]);

  const load = useCallback(async (sid = snapshotId, aq = activeQ, ae = activeEnabled, p = page) => {
    if (!sid) return;
    setLoading(true);
    try {
      const enabled = ae === 'true' ? true : ae === 'false' ? false : undefined;
      const r = await cargasApi.getRegistrosEntraID(sid, { q: aq || undefined, accountEnabled: enabled, page: p, pageSize: PAGE_SIZE });
      setData(r.items); setTotal(r.total);
    } catch { toast.error('Error al cargar registros Entra ID'); } finally { setLoading(false); }
  }, [snapshotId, activeQ, activeEnabled, page]);

  useEffect(() => { if (snapshotId) load(); }, [snapshotId, refreshKey]);

  const buscar = () => { setActiveQ(q); setActiveEnabled(accountEnabled); setPage(1); load(snapshotId, q, accountEnabled, 1); };
  const limpiar = () => { setQ(''); setAccountEnabled(''); setActiveQ(''); setActiveEnabled(''); setPage(1); load(snapshotId, '', '', 1); };
  const irPagina = (p: number) => { setPage(p); load(snapshotId, activeQ, activeEnabled, p); };

  return (
    <div className="flex flex-col gap-4">
      <div className="flex gap-2 flex-wrap items-center">
        <select value={snapshotId} onChange={e => { setSnapshotId(e.target.value); setPage(1); setActiveQ(''); setActiveEnabled(''); }}
          className="border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-400 bg-white min-w-[200px]">
          {snapshots.map(s => (
            <option key={s.id} value={s.id}>{s.nombre} ({s.totalRegistros.toLocaleString()} usuarios)</option>
          ))}
        </select>
        <div className="relative flex-1 min-w-[160px]">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && buscar()}
            placeholder="Nombre, cédula o UPN…"
            className="w-full pl-8 pr-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-sky-400" />
        </div>
        <select value={accountEnabled} onChange={e => setAccountEnabled(e.target.value)}
          className="border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none bg-white">
          <option value="">Todas las cuentas</option>
          <option value="true">Habilitadas</option>
          <option value="false">Deshabilitadas</option>
        </select>
        <button onClick={buscar} className="flex items-center gap-1.5 bg-sky-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-sky-700 transition">
          <Search size={13} />Buscar</button>
        {(activeQ || activeEnabled) && <button onClick={limpiar} className="text-sm text-gray-500 hover:text-gray-700 px-2">✕ Limpiar</button>}
      </div>
      <VisorTable loading={loading} empty={total === 0 && !snapshotId} emptyMsg={!snapshotId ? 'No hay snapshots cargados.' : undefined}
        headers={['Cédula (EmpID)','Nombre','UPN / Email','Depto','Cargo','Cuenta','Último Sign-in']}>
        {data.map((r, i) => (
          <tr key={i} className="hover:bg-sky-50/40 transition-colors">
            <td className="px-3 py-2.5 font-mono text-[12px] font-medium text-sky-700">{r.employeeId ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-800 text-sm font-medium">{r.displayName ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-500 text-xs">{r.userPrincipalName ?? r.email ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-500 text-xs">{r.department ?? '—'}</td>
            <td className="px-3 py-2.5 text-gray-500 text-xs">{r.jobTitle ?? '—'}</td>
            <td className="px-3 py-2.5">
              {r.accountEnabled
                ? <span className="flex items-center gap-1 text-[11px] text-green-700 font-medium"><CheckCircle2 size={11} />Habilitada</span>
                : <span className="flex items-center gap-1 text-[11px] text-red-600 font-medium"><XCircle size={11} />Deshabilitada</span>}
            </td>
            <td className="px-3 py-2.5 text-gray-400 text-xs whitespace-nowrap">{r.ultimoSignIn ?? '—'}</td>
          </tr>
        ))}
      </VisorTable>
      <Pager page={page} total={total} pageSize={PAGE_SIZE} onPage={irPagina} />
    </div>
  );
}

// ─── Tabla base ───────────────────────────────────────────────────────────────
function VisorTable({ loading, empty, emptyMsg, headers, children }: {
  loading: boolean; empty: boolean; emptyMsg?: string; headers: string[]; children: React.ReactNode;
}) {
  if (loading) return (
    <div className="flex items-center justify-center py-16 text-gray-400 text-sm gap-2 bg-white rounded-xl border border-gray-100">
      <RefreshCw size={16} className="animate-spin" />Cargando registros…
    </div>
  );
  if (empty) return (
    <div className="text-center py-16 text-gray-400 text-sm border border-dashed border-gray-200 rounded-xl bg-gray-50/50">
      {emptyMsg ?? 'No hay registros. Carga un archivo para comenzar.'}
    </div>
  );
  return (
    <div className="border border-gray-200 rounded-xl overflow-hidden shadow-sm">
      <div className="overflow-x-auto">
        <table className="w-full text-xs min-w-max">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>{headers.map(h => <th key={h} className="px-3 py-2.5 text-left font-semibold text-gray-600 whitespace-nowrap">{h}</th>)}</tr>
          </thead>
          <tbody className="divide-y divide-gray-100 bg-white">{children}</tbody>
        </table>
      </div>
    </div>
  );
}

// ─── Modal de importación ─────────────────────────────────────────────────────
const SISTEMAS = ['SAP', 'EVOLUTION', 'SE_SUITE', 'AD', 'OTRO'];
interface SociedadOpcion { id: number; codigo: string; nombre: string; }

function ImportarModal({
  tipoInicial, onClose, onSuccess,
}: { tipoInicial: TipoCarga; onClose: () => void; onSuccess: (tipo: TipoCarga) => void }) {
  const [tipo, setTipo] = useState<TipoCarga>(tipoInicial);
  const [file, setFile] = useState<File | null>(null);
  const [resultado, setResultado] = useState<CargaResultado | null>(null);
  const [loading, setLoading] = useState(false);
  const [sistema, setSistema] = useState('SAP');
  const [sociedadCodigo, setSociedadCodigo] = useState('');
  const [sociedades, setSociedades] = useState<SociedadOpcion[]>([]);
  const [nombreSnapshot, setNombreSnapshot] = useState('');
  const [syncLoading, setSyncLoading] = useState(false);
  const [ultimoSnapshot, setUltimoSnapshot] = useState<{ id: string; nombre: string; origen?: string } | null>(null);
  const [snapshots, setSnapshots] = useState<SnapshotEntraIDDto[]>([]);
  const [descargandoId, setDescargandoId] = useState<string | null>(null);
  const cfg = TIPO_CONFIG[tipo];

  useEffect(() => {
    api.get('/sociedades').then(r => setSociedades(r.data)).catch(() => {});
    if (tipo === 'entraID') cargasApi.getSnapshotsEntraID().then(setSnapshots).catch(() => {});
  }, [tipo]);

  const clearResultado = () => { setResultado(null); setUltimoSnapshot(null); };

  const descargarPlantilla = async () => {
    try {
      const fn = tipo === 'empleados' ? cargasApi.descargarPlantillaEmpleados
        : tipo === 'sapRoles' ? cargasApi.descargarPlantillaSapRoles
        : tipo === 'matrizPuestos' ? cargasApi.descargarPlantillaMatrizPuestos
        : tipo === 'entraID' ? cargasApi.descargarPlantillaEntraID
        : cargasApi.descargarPlantillaCasosSeSuite;
      await fn();
    } catch { toast.error('Error al descargar la plantilla'); }
  };

  const handleCarga = async () => {
    if (!file) { toast.error('Selecciona un archivo primero'); return; }
    setLoading(true);
    try {
      const cod = sociedadCodigo || undefined;
      if (tipo === 'entraID') {
        const res = await cargasApi.cargarSnapshotEntraID(file, nombreSnapshot || undefined);
        setResultado({ totalRegistros: res.totalRegistros, insertados: res.totalRegistros - res.errores, actualizados: 0, errores: res.errores, detalleErrores: res.detalleErrores });
        setUltimoSnapshot({ id: res.snapshotId, nombre: res.nombre });
        if (res.errores === 0) { toast.success(`Snapshot "${res.nombre}" insertado: ${res.totalRegistros} usuarios`); onSuccess('entraID'); }
        else toast.warning(`Snapshot insertado con ${res.errores} error(es)`);
        setFile(null); setNombreSnapshot('');
        cargasApi.getSnapshotsEntraID().then(setSnapshots).catch(() => {});
        return;
      }
      const r = tipo === 'empleados' ? await cargasApi.cargarEmpleados(file, cod)
        : tipo === 'sapRoles' ? await cargasApi.cargarRolesSAP(file, sistema, cod)
        : tipo === 'matrizPuestos' ? await cargasApi.cargarMatrizPuestos(file, cod)
        : await cargasApi.cargarCasosSeSuite(file, cod);
      setResultado(r); setFile(null);
      if (r.errores === 0) { toast.success(`Carga completada: ${r.insertados} nuevos, ${r.actualizados} actualizados`); onSuccess(tipo); }
      else toast.warning(`Carga con ${r.errores} error(es)`);
    } catch { toast.error('Error al procesar el archivo'); } finally { setLoading(false); }
  };

  const handleSyncDirecto = async () => {
    setSyncLoading(true); setResultado(null); setUltimoSnapshot(null);
    try {
      const res = await cargasApi.syncEntraIDDirecto(nombreSnapshot || undefined);
      setResultado({ totalRegistros: res.totalRegistros, insertados: res.totalRegistros - res.errores, actualizados: 0, errores: res.errores, detalleErrores: res.detalleErrores });
      setUltimoSnapshot({ id: res.snapshotId, nombre: res.nombre, origen: res.origen });
      if (res.errores === 0) { toast.success(`Snapshot "${res.nombre}" sincronizado: ${res.totalRegistros} usuarios`); onSuccess('entraID'); }
      else toast.warning(`Sync con ${res.errores} error(es)`);
      cargasApi.getSnapshotsEntraID().then(setSnapshots).catch(() => {}); setNombreSnapshot('');
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors?.[0];
      toast.error(msg ?? 'Error al sincronizar con Microsoft Graph.');
    } finally { setSyncLoading(false); }
  };

  const descargarSnapshot = async (snap: SnapshotEntraIDDto) => {
    setDescargandoId(snap.id);
    try { await cargasApi.descargarSnapshotEntraID(snap.id, snap.nombre); toast.success(`Descargando "${snap.nombre}"`); }
    catch { toast.error('Error al descargar el snapshot'); } finally { setDescargandoId(null); }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-end bg-black/40" onClick={onClose}>
      <div className="h-full w-full max-w-xl bg-white shadow-2xl overflow-y-auto flex flex-col" onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b bg-gray-50 sticky top-0 z-10">
          <div className="flex items-center gap-2">
            <ArrowUpFromLine size={18} className="text-blue-600" />
            <h2 className="font-bold text-gray-900">Importar datos</h2>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 transition"><X size={20} /></button>
        </div>

        <div className="p-6 space-y-5 flex-1">
          {/* Selector tipo */}
          <div>
            <label className="text-xs font-semibold text-gray-600 mb-2 block">Tipo de datos</label>
            <div className="grid grid-cols-1 gap-1.5">
              {(Object.entries(TIPO_CONFIG) as [TipoCarga, typeof TIPO_CONFIG[TipoCarga]][]).map(([key, c]) => (
                <button key={key} onClick={() => { setTipo(key); setFile(null); setResultado(null); }}
                  className={`flex items-center gap-3 p-3 rounded-xl border transition text-left ${tipo === key ? `${c.bgColor} ${c.borderColor} ${c.color}` : 'border-gray-200 hover:border-gray-300 bg-white'}`}>
                  <span className={tipo === key ? c.color : 'text-gray-400'}>{c.icon}</span>
                  <div>
                    <p className={`font-semibold text-sm ${tipo === key ? c.color : 'text-gray-700'}`}>{c.label}</p>
                  </div>
                </button>
              ))}
            </div>
          </div>

          {/* Columnas + plantilla */}
          <div className={`rounded-xl border p-4 ${cfg.bgColor} ${cfg.borderColor}`}>
            <div className="flex items-center justify-between mb-2">
              <p className="text-xs font-semibold text-gray-700">Columnas requeridas</p>
              <button onClick={descargarPlantilla}
                className="flex items-center gap-1 text-xs text-blue-600 hover:underline font-medium">
                <Download size={11} /> Descargar plantilla
              </button>
            </div>
            <div className="flex flex-wrap gap-1">
              {cfg.columnas.map(col => (
                <span key={col} className={`px-1.5 py-0.5 rounded text-[11px] font-mono ${col.endsWith('*') ? 'bg-blue-100 text-blue-700' : 'bg-white/80 text-gray-600'}`}>{col}</span>
              ))}
            </div>
            <p className="text-[11px] text-gray-500 mt-2">* Obligatorios</p>
          </div>

          {/* SAP sistema */}
          {tipo === 'sapRoles' && (
            <div className="flex items-center gap-3">
              <label className="text-sm font-medium text-gray-700 whitespace-nowrap">Sistema origen:</label>
              <select value={sistema} onChange={e => setSistema(e.target.value)}
                className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                {SISTEMAS.map(s => <option key={s}>{s}</option>)}
              </select>
            </div>
          )}

          {/* Sociedad */}
          {tipo !== 'entraID' && (
            <div className="flex items-center gap-3">
              <Building2 size={15} className="text-gray-500 shrink-0" />
              <label className="text-sm font-medium text-gray-700 whitespace-nowrap">Sociedad SAP:</label>
              <select value={sociedadCodigo} onChange={e => setSociedadCodigo(e.target.value)}
                className="flex-1 border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="">— Sin especificar —</option>
                {sociedades.map(s => <option key={s.codigo} value={s.codigo}>{s.codigo} · {s.nombre}</option>)}
              </select>
            </div>
          )}

          {/* Entra ID: sync directa */}
          {tipo === 'entraID' && (
            <div className="bg-sky-50 border border-sky-200 rounded-xl p-4 space-y-3">
              <div className="flex items-center gap-2 text-sky-700 font-semibold text-sm"><Zap size={15} />Sincronización directa — Microsoft Graph</div>
              <p className="text-xs text-gray-500">Conecta al tenant Azure AD vía Managed Identity. No requiere exportar Excel.</p>
              <input type="text" value={nombreSnapshot} onChange={e => setNombreSnapshot(e.target.value)}
                placeholder='Nombre del snapshot (opcional)'
                className="w-full border border-sky-300 bg-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500" />
              <button onClick={handleSyncDirecto} disabled={syncLoading || loading}
                className="w-full flex items-center justify-center gap-2 bg-sky-600 text-white px-5 py-2.5 rounded-lg text-sm font-semibold hover:bg-sky-700 disabled:opacity-50 transition">
                {syncLoading ? <><RefreshCw size={14} className="animate-spin" />Sincronizando…</> : <><Zap size={14} />Sincronizar Entra ID</>}
              </button>
              <p className="text-[11px] text-sky-600 flex items-center gap-1"><CheckCircle2 size={10} />Requiere <span className="font-mono bg-sky-100 px-1 rounded">User.Read.All</span> en la Managed Identity</p>
            </div>
          )}

          {/* Divisor Entra ID */}
          {tipo === 'entraID' && (
            <div className="relative flex items-center">
              <div className="flex-grow border-t border-gray-200" />
              <span className="mx-3 text-xs text-gray-400">o importa desde Excel</span>
              <div className="flex-grow border-t border-gray-200" />
            </div>
          )}

          {/* Drop zone */}
          <DropZone onFile={setFile} file={file} onClear={() => setFile(null)} loading={loading} />

          {/* Botón procesar */}
          {file && (
            <button onClick={handleCarga} disabled={loading || syncLoading}
              className={`w-full flex items-center justify-center gap-2 text-white px-5 py-3 rounded-xl text-sm font-semibold disabled:opacity-50 transition ${tipo === 'entraID' ? 'bg-slate-700 hover:bg-slate-800' : 'bg-blue-600 hover:bg-blue-700'}`}>
              {loading ? <><RefreshCw size={15} className="animate-spin" />Procesando…</> : <><Upload size={15} />{tipo === 'entraID' ? 'Importar desde Excel' : `Cargar ${cfg.label}`}</>}
            </button>
          )}

          {/* Resultado */}
          {resultado && (
            <div className="space-y-3">
              <ResultadoPanel resultado={resultado} onClose={clearResultado} />
              {tipo === 'entraID' && ultimoSnapshot && (
                <div className="flex items-center gap-3 bg-sky-50 border border-sky-200 rounded-xl px-4 py-3">
                  <CheckCircle2 size={15} className="text-sky-600 shrink-0" />
                  <div className="flex-1 text-sm text-sky-800">
                    <span className="font-semibold">"{ultimoSnapshot.nombre}"</span>
                    {ultimoSnapshot.origen === 'GRAPH_DIRECT'
                      ? <span className="ml-2 text-[11px] bg-sky-200 text-sky-800 px-1.5 py-0.5 rounded font-mono"><Zap size={9} className="inline mr-0.5" />GRAPH_DIRECT</span>
                      : <span className="ml-2 text-[11px] bg-slate-200 text-slate-700 px-1.5 py-0.5 rounded font-mono"><DatabaseZap size={9} className="inline mr-0.5" />MANUAL_EXCEL</span>}
                  </div>
                  <button onClick={() => descargarSnapshot({ id: ultimoSnapshot.id, nombre: ultimoSnapshot.nombre, fechaInstantanea: new Date().toISOString(), totalRegistros: resultado.totalRegistros })}
                    disabled={descargandoId === ultimoSnapshot.id}
                    className="flex items-center gap-1 bg-sky-600 text-white px-3 py-1.5 rounded-lg text-xs font-medium hover:bg-sky-700 disabled:opacity-50 transition">
                    {descargandoId === ultimoSnapshot.id ? <RefreshCw size={12} className="animate-spin" /> : <Download size={12} />}Excel
                  </button>
                </div>
              )}
            </div>
          )}

          {/* Historial snapshots Entra ID */}
          {tipo === 'entraID' && snapshots.length > 0 && (
            <div>
              <p className="text-xs font-semibold text-gray-600 mb-2">Snapshots disponibles</p>
              <div className="border border-gray-200 rounded-xl overflow-hidden">
                <table className="w-full text-xs">
                  <thead className="bg-gray-50 border-b"><tr>
                    <th className="px-3 py-2 text-left font-semibold text-gray-600">Nombre</th>
                    <th className="px-3 py-2 text-left font-semibold text-gray-600">Fecha</th>
                    <th className="px-3 py-2 text-right font-semibold text-gray-600">Usuarios</th>
                    <th className="px-3 py-2"></th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100">
                    {snapshots.map(s => (
                      <tr key={s.id} className="hover:bg-gray-50">
                        <td className="px-3 py-2 font-medium text-gray-800">{s.nombre}</td>
                        <td className="px-3 py-2 text-gray-500">{new Date(s.fechaInstantanea).toLocaleDateString('es-CR')}</td>
                        <td className="px-3 py-2 text-right font-mono text-sky-700">{s.totalRegistros.toLocaleString()}</td>
                        <td className="px-3 py-2 text-right">
                          <button onClick={() => descargarSnapshot(s)} disabled={descargandoId === s.id}
                            className="text-blue-600 hover:text-blue-800 disabled:opacity-50 transition">
                            {descargandoId === s.id ? <RefreshCw size={12} className="animate-spin" /> : <Download size={12} />}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Page principal ───────────────────────────────────────────────────────────
export function Cargas() {
  const [tab, setTab] = useState<TipoCarga>('empleados');
  const [showImportar, setShowImportar] = useState(false);
  const [refreshKeys, setRefreshKeys] = useState<Record<TipoCarga, number>>({
    empleados: 0, sapRoles: 0, matrizPuestos: 0, casosSeSuite: 0, entraID: 0,
  });
  const [lotes, setLotes] = useState<LoteCargaDto[]>([]);
  const [snapshots, setSnapshots] = useState<SnapshotEntraIDDto[]>([]);
  const [showLotes, setShowLotes] = useState(false);
  const [showPurgar, setShowPurgar] = useState(false);
  const [purgarConfirm, setPurgarConfirm] = useState('');
  const [purgarLoading, setPurgarLoading] = useState(false);

  useEffect(() => {
    cargasApi.getLotes({ limit: 100 }).then(setLotes).catch(() => {});
    cargasApi.getSnapshotsEntraID().then(setSnapshots).catch(() => {});
  }, [refreshKeys]);

  const handleSuccess = (tipo: TipoCarga) => {
    setRefreshKeys(k => ({ ...k, [tipo]: k[tipo] + 1 }));
    cargasApi.getLotes({ limit: 100 }).then(setLotes).catch(() => {});
    cargasApi.getSnapshotsEntraID().then(setSnapshots).catch(() => {});
    setShowImportar(false);
  };

  const handlePurgar = async () => {
    if (purgarConfirm !== 'ELIMINAR') return;
    setPurgarLoading(true);
    try {
      const res = await cargasApi.purgarCargasAntiguas();
      toast.success(`Purga completada: ${res.lotesBorrados} lotes · ${res.registrosBorrados} registros eliminados.`);
      setShowPurgar(false); setPurgarConfirm('');
      cargasApi.getLotes({ limit: 100 }).then(setLotes).catch(() => {});
    } catch { toast.error('Error al purgar cargas antiguas.'); } finally { setPurgarLoading(false); }
  };

  const ultimoLotePorTipo = (tipo: string) => lotes.find(l => l.tipoCarga === tipo && l.esVigente);
  const conteoTipo = (tipo: string) => ultimoLotePorTipo(tipo)?.totalRegistros ?? 0;
  const TIPO_MAP: Record<TipoCarga, string> = { empleados: 'EMPLEADOS', sapRoles: 'SAP_ROLES', matrizPuestos: 'MATRIZ_PUESTOS', casosSeSuite: 'CASOS_SESUITE', entraID: 'ENTRA_ID' };

  const loteActual = ultimoLotePorTipo(TIPO_MAP[tab]);
  const cfg = TIPO_CONFIG[tab];

  return (
    <div className="flex flex-col h-full bg-gray-50">
      {/* ── Header ───────────────────────────────────────────────────────────── */}
      <div className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-gray-900">Cargas Masivas</h1>
            <p className="text-xs text-gray-500 mt-0.5">Datos maestros del sistema — consulta, filtra e importa</p>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={() => setShowLotes(!showLotes)}
              className="flex items-center gap-1.5 px-3 py-2 text-sm text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition">
              <Clock size={14} />{showLotes ? 'Ocultar historial' : 'Historial'}
            </button>
            <button onClick={() => { setShowPurgar(true); setPurgarConfirm(''); }}
              className="flex items-center gap-1.5 px-3 py-2 text-sm text-red-600 border border-red-200 rounded-lg hover:bg-red-50 transition">
              <Trash2 size={14} />Purgar
            </button>
            <button onClick={() => setShowImportar(true)}
              className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-blue-700 transition shadow-sm">
              <ArrowUpFromLine size={15} />Importar datos
            </button>
          </div>
        </div>

        {/* Tabs con conteo */}
        <div className="flex gap-1.5 mt-4 overflow-x-auto pb-1">
          {(Object.entries(TIPO_CONFIG) as [TipoCarga, typeof TIPO_CONFIG[TipoCarga]][]).map(([key, c]) => {
            const count = key === 'entraID' ? (snapshots[0]?.totalRegistros ?? 0) : conteoTipo(TIPO_MAP[key]);
            const isActive = tab === key;
            return (
              <button key={key} onClick={() => setTab(key)}
                className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition whitespace-nowrap border ${
                  isActive ? `${c.bgColor} ${c.borderColor} ${c.color}` : 'bg-white border-gray-200 text-gray-600 hover:bg-gray-50'
                }`}>
                <span className={isActive ? c.color : 'text-gray-400'}>{c.icon}</span>
                <span>{c.labelCorto}</span>
                {count > 0 && (
                  <span className={`px-2 py-0.5 rounded-full text-[11px] font-bold ${isActive ? 'bg-white/60' : 'bg-gray-100 text-gray-500'}`}>
                    {count.toLocaleString()}
                  </span>
                )}
              </button>
            );
          })}
        </div>
      </div>

      {/* ── Banner estado carga actual ───────────────────────────────────────── */}
      {loteActual && (
        <div className={`px-6 py-2.5 border-b ${cfg.bgColor} ${cfg.borderColor} border-b flex items-center gap-4 text-xs`}>
          <BadgeCheck size={14} className={cfg.color} />
          <span className="text-gray-700 font-medium">Último lote vigente:</span>
          <span className="text-gray-600">{fmtFecha(loteActual.fechaCarga)}</span>
          {loteActual.nombreArchivo && <span className="text-gray-500 font-mono">{loteActual.nombreArchivo}</span>}
          <span className="text-green-700 font-semibold flex items-center gap-1">
            <CheckCircle2 size={11} />{loteActual.totalRegistros.toLocaleString()} registros · {loteActual.insertados.toLocaleString()} ins · {loteActual.actualizados.toLocaleString()} act
            {loteActual.errores > 0 && <span className="text-red-600 ml-1">· {loteActual.errores} err</span>}
          </span>
          <button onClick={() => setShowImportar(true)}
            className={`ml-auto flex items-center gap-1 px-3 py-1 rounded-lg text-xs font-medium ${cfg.color} border ${cfg.borderColor} ${cfg.bgColor} hover:opacity-80 transition`}>
            <ArrowUpFromLine size={11} />Nueva carga
          </button>
        </div>
      )}
      {!loteActual && (
        <div className="px-6 py-2.5 border-b bg-yellow-50 border-yellow-200 flex items-center gap-3 text-xs">
          <AlertTriangle size={13} className="text-yellow-600" />
          <span className="text-yellow-800 font-medium">No hay datos cargados para este tipo.</span>
          <button onClick={() => setShowImportar(true)}
            className="ml-auto flex items-center gap-1 px-3 py-1 rounded-lg text-xs font-medium text-yellow-800 border border-yellow-300 bg-yellow-100 hover:bg-yellow-200 transition">
            <ArrowUpFromLine size={11} />Importar ahora
          </button>
        </div>
      )}

      {/* ── Visor ────────────────────────────────────────────────────────────── */}
      <div className="flex-1 overflow-auto p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Filter size={14} className={cfg.color} />
            <h2 className={`text-sm font-semibold ${cfg.color}`}>{cfg.label}</h2>
            <span className="text-xs text-gray-400">— filtra y explora los registros cargados</span>
          </div>
          <button onClick={() => setRefreshKeys(k => ({ ...k, [tab]: k[tab] + 1 }))}
            className="flex items-center gap-1 text-xs text-gray-500 hover:text-gray-700 transition">
            <RefreshCw size={12} />Refrescar
          </button>
        </div>

        {tab === 'empleados'    && <VisorEmpleados refreshKey={refreshKeys.empleados} />}
        {tab === 'sapRoles'     && <VisorSAPRoles refreshKey={refreshKeys.sapRoles} />}
        {tab === 'matrizPuestos' && <VisorMatriz refreshKey={refreshKeys.matrizPuestos} />}
        {tab === 'casosSeSuite' && <VisorCasosSE refreshKey={refreshKeys.casosSeSuite} />}
        {tab === 'entraID'      && <VisorEntraID snapshots={snapshots} refreshKey={refreshKeys.entraID} />}
      </div>

      {/* ── Historial de lotes (desplegable) ─────────────────────────────────── */}
      {showLotes && (
        <div className="border-t border-gray-200 bg-white p-6">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-semibold text-gray-700">Historial de cargas</h2>
            <button onClick={() => setShowLotes(false)} className="text-gray-400 hover:text-gray-600"><X size={16} /></button>
          </div>
          <div className="border border-gray-200 rounded-xl overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-xs min-w-max">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>{['Tipo','Sociedad','Archivo','Fecha','Total','Insert.','Actual.','Errores','Estado'].map(h =>
                    <th key={h} className="px-3 py-2.5 text-left font-semibold text-gray-600 whitespace-nowrap">{h}</th>)}
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 bg-white">
                  {lotes.map(l => (
                    <tr key={l.id} className={`hover:bg-gray-50 ${!l.esVigente ? 'opacity-50' : ''}`}>
                      <td className="px-3 py-2"><span className={`px-2 py-0.5 rounded-full text-[11px] font-medium ${TIPO_COLOR[l.tipoCarga] ?? 'bg-gray-100 text-gray-600'}`}>{TIPO_LABEL[l.tipoCarga] ?? l.tipoCarga}</span></td>
                      <td className="px-3 py-2 text-gray-600">{l.sociedadNombre ?? '—'}</td>
                      <td className="px-3 py-2 text-gray-500 max-w-[140px] truncate" title={l.nombreArchivo ?? ''}>{l.nombreArchivo ?? '—'}</td>
                      <td className="px-3 py-2 text-gray-600 whitespace-nowrap"><Clock size={10} className="inline mr-1 text-gray-400" />{fmtFecha(l.fechaCarga)}</td>
                      <td className="px-3 py-2 font-mono text-gray-700">{l.totalRegistros.toLocaleString()}</td>
                      <td className="px-3 py-2 font-mono text-green-700">{l.insertados.toLocaleString()}</td>
                      <td className="px-3 py-2 font-mono text-blue-700">{l.actualizados.toLocaleString()}</td>
                      <td className="px-3 py-2 font-mono"><span className={l.errores > 0 ? 'text-red-600' : 'text-gray-400'}>{l.errores}</span></td>
                      <td className="px-3 py-2">
                        {l.esVigente ? <span className="flex items-center gap-1 text-green-700 font-medium"><BadgeCheck size={12} />Vigente</span> : <span className="text-gray-400">Anterior</span>}
                      </td>
                    </tr>
                  ))}
                  {lotes.length === 0 && <tr><td colSpan={9} className="px-3 py-8 text-center text-gray-400">No hay lotes registrados</td></tr>}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* ── Modal importar ────────────────────────────────────────────────────── */}
      {showImportar && (
        <ImportarModal tipoInicial={tab} onClose={() => setShowImportar(false)} onSuccess={handleSuccess} />
      )}

      {/* ── Modal purgar ─────────────────────────────────────────────────────── */}
      {showPurgar && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
            <div className="flex items-start gap-3 mb-4">
              <AlertOctagon size={22} className="text-red-500 mt-0.5 shrink-0" />
              <div>
                <h3 className="font-semibold text-gray-900 text-base">Purgar cargas antiguas</h3>
                <p className="text-sm text-gray-500 mt-1">
                  Elimina todos los lotes anteriores de cada tipo, conservando solo el más reciente. <strong>Esta acción no se puede deshacer.</strong>
                </p>
              </div>
            </div>
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Escribe <span className="font-mono font-bold text-red-600">ELIMINAR</span> para confirmar
              </label>
              <input type="text" value={purgarConfirm} onChange={e => setPurgarConfirm(e.target.value)}
                placeholder="ELIMINAR" autoFocus
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-red-300" />
            </div>
            <div className="flex justify-end gap-2">
              <button onClick={() => setShowPurgar(false)}
                className="px-4 py-2 text-sm text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition">Cancelar</button>
              <button onClick={handlePurgar} disabled={purgarConfirm !== 'ELIMINAR' || purgarLoading}
                className="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-40 disabled:cursor-not-allowed transition">
                {purgarLoading ? <RefreshCw size={14} className="animate-spin" /> : <Trash2 size={14} />}
                {purgarLoading ? 'Eliminando…' : 'Eliminar cargas antiguas'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
