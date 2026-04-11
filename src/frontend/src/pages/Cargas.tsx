import { useState, useRef } from 'react';
import { cargasApi, type CargaResultado } from '../api/cargas';
import { toast } from 'sonner';
import {
  Upload, Download, FileSpreadsheet, Users, UserCog,
  CheckCircle2, XCircle, AlertTriangle, RefreshCw, X
} from 'lucide-react';

type TipoCarga = 'empleados' | 'usuarios';

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
  usuarios: {
    label: 'Usuarios de Sistema',
    descripcion: 'Importa cuentas de usuario del directorio activo o sistemas de aplicación.',
    icon: <UserCog size={20} className="text-purple-600" />,
    columnas: ['NombreUsuario*', 'SociedadCodigo*', 'TipoUsuario (INTERNO/EXTERNO/SERVICIO)',
               'Estado (ACTIVO/BLOQUEADO/ELIMINADO)', 'FechaCreacion (YYYY-MM-DD)',
               'SistemaOrigen', 'NombreMostrar'],
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
export function Cargas() {
  const [tipo, setTipo] = useState<TipoCarga>('empleados');
  const [state, setState] = useState<CargaState>({ file: null, resultado: null, loading: false, dragging: false });
  const cfg = TIPO_CONFIG[tipo];

  const setFile = (file: File) => setState(s => ({ ...s, file, resultado: null }));
  const clearFile = () => setState(s => ({ ...s, file: null, resultado: null }));
  const clearResultado = () => setState(s => ({ ...s, resultado: null }));

  const handleCarga = async () => {
    if (!state.file) { toast.error('Selecciona un archivo primero'); return; }
    setState(s => ({ ...s, loading: true, resultado: null }));
    try {
      const fn = tipo === 'empleados' ? cargasApi.cargarEmpleados : cargasApi.cargarUsuarios;
      const resultado = await fn(state.file, 1);
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
      const fn = tipo === 'empleados' ? cargasApi.descargarPlantillaEmpleados : cargasApi.descargarPlantillaUsuarios;
      await fn();
    } catch {
      toast.error('Error al descargar la plantilla');
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
          <button key={key} onClick={() => { setTipo(key); clearFile(); clearResultado(); }}
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
          <button onClick={handleCarga} disabled={state.loading}
            className="flex items-center gap-2 bg-blue-600 text-white px-5 py-2.5 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition">
            {state.loading ? <RefreshCw size={15} className="animate-spin" /> : <Upload size={15} />}
            {state.loading ? 'Procesando...' : `Cargar ${cfg.label}`}
          </button>
          <button onClick={clearFile} className="text-sm text-gray-500 hover:text-gray-700 transition">
            Cancelar
          </button>
        </div>
      )}

      {/* Resultado */}
      {state.resultado && (
        <div className="mt-5">
          <ResultadoPanel resultado={state.resultado} onClose={clearResultado} />
        </div>
      )}
    </div>
  );
}
