import { useState, useEffect, useRef } from 'react';
import { baseConocimientoApi, type BaseConocimientoDto, type IngestResultado } from '../api/baseConocimiento';
import { toast } from 'sonner';
import {
  Database, FolderOpen, Upload, Search, Trash2, RefreshCw,
  FileText, FileSpreadsheet, File, CheckCircle2, AlertTriangle,
  BookOpen, ChevronDown, ChevronUp, X, Tag
} from 'lucide-react';

// ─── Helpers ──────────────────────────────────────────────────────────────────
const TIPO_ICON: Record<string, React.ReactNode> = {
  PDF:  <FileText     size={16} className="text-red-500" />,
  DOCX: <FileText     size={16} className="text-blue-500" />,
  DOC:  <FileText     size={16} className="text-blue-500" />,
  XLSX: <FileSpreadsheet size={16} className="text-green-600" />,
  XLS:  <FileSpreadsheet size={16} className="text-green-600" />,
  CSV:  <FileSpreadsheet size={16} className="text-green-500" />,
  TXT:  <File          size={16} className="text-gray-400" />,
  MD:   <File          size={16} className="text-gray-400" />,
};

function formatBytes(bytes: number) {
  if (bytes === 0) return '—';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 ** 2) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 ** 2).toFixed(1)} MB`;
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('es-CR', { dateStyle: 'short', timeStyle: 'short' });
}

function parseTags(json: string | null): string[] {
  if (!json) return [];
  try { return JSON.parse(json); } catch { return []; }
}

// ─── Panel de resultado de ingesta ────────────────────────────────────────────
function ResultPanel({ r, onClose }: { r: IngestResultado; onClose: () => void }) {
  const [expanded, setExpanded] = useState(false);
  const ok = r.errores === 0;
  return (
    <div className={`rounded-xl border p-4 ${ok ? 'bg-green-50 border-green-200' : 'bg-yellow-50 border-yellow-200'}`}>
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          {ok ? <CheckCircle2 size={18} className="text-green-600" /> : <AlertTriangle size={18} className="text-yellow-600" />}
          <span className="font-semibold text-sm text-gray-800">
            {r.procesados} procesados · {r.omitidos} omitidos · {r.errores} errores
          </span>
        </div>
        <button onClick={onClose}><X size={15} className="text-gray-400 hover:text-gray-600" /></button>
      </div>
      {r.detalles.length > 0 && (
        <>
          <button onClick={() => setExpanded(e => !e)}
            className="text-xs text-blue-600 flex items-center gap-1 hover:underline">
            {expanded ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
            {expanded ? 'Ocultar detalle' : `Ver detalle (${r.detalles.length} entradas)`}
          </button>
          {expanded && (
            <div className="mt-2 max-h-40 overflow-y-auto bg-white rounded-lg border text-xs p-2 space-y-0.5">
              {r.detalles.map((d, i) => (
                <div key={i} className={d.startsWith('ERROR') ? 'text-red-600' : 'text-gray-600'}>{d}</div>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}

// ─── Fila de documento ────────────────────────────────────────────────────────
function DocRow({ doc, onDelete }: { doc: BaseConocimientoDto; onDelete: (id: string) => void }) {
  const [expanded, setExpanded] = useState(false);
  const tags = parseTags(doc.tags);
  const controles = parseTags(doc.controlesDetectados);

  return (
    <>
      <tr
        className={`cursor-pointer hover:bg-gray-50 transition-colors ${expanded ? 'bg-blue-50' : ''}`}
        onClick={() => setExpanded(e => !e)}
      >
        <td className="px-4 py-3">
          <div className="flex items-center gap-2">
            {TIPO_ICON[doc.tipoArchivo] ?? <File size={16} className="text-gray-400" />}
            <span className="text-xs font-medium text-gray-700 max-w-[200px] truncate" title={doc.nombreArchivo}>
              {doc.nombreArchivo}
            </span>
          </div>
        </td>
        <td className="px-4 py-3">
          {doc.dominioDetectado
            ? <span className="px-2 py-0.5 rounded-full text-[11px] font-medium bg-blue-100 text-blue-700">{doc.dominioDetectado}</span>
            : <span className="text-xs text-gray-400">—</span>}
        </td>
        <td className="px-4 py-3 text-xs text-gray-500">{doc.tipoArchivo}</td>
        <td className="px-4 py-3 text-xs text-gray-500">{formatBytes(doc.tamanoBytes)}</td>
        <td className="px-4 py-3 text-xs text-gray-500">{doc.totalPalabras.toLocaleString()} palabras</td>
        <td className="px-4 py-3 text-xs text-gray-400">{formatDate(doc.creadoAt)}</td>
        <td className="px-4 py-3">
          <div className="flex items-center gap-2">
            <button
              onClick={e => { e.stopPropagation(); onDelete(doc.id); }}
              className="text-gray-300 hover:text-red-500 transition">
              <Trash2 size={14} />
            </button>
            {expanded ? <ChevronUp size={14} className="text-gray-400" /> : <ChevronDown size={14} className="text-gray-400" />}
          </div>
        </td>
      </tr>
      {expanded && (
        <tr className="bg-blue-50 border-b border-blue-100">
          <td colSpan={7} className="px-6 py-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-xs">
              <div>
                <p className="font-semibold text-gray-600 mb-1">Resumen del contenido</p>
                <p className="text-gray-600 leading-relaxed">{doc.resumen || '—'}</p>
              </div>
              <div>
                <p className="font-semibold text-gray-600 mb-1 flex items-center gap-1">
                  <Tag size={11} /> Controles detectados
                </p>
                {controles.length > 0
                  ? <div className="flex flex-wrap gap-1">{controles.map(c =>
                      <span key={c} className="px-1.5 py-0.5 rounded bg-orange-100 text-orange-700 font-mono">{c}</span>
                    )}</div>
                  : <span className="text-gray-400">Ninguno detectado</span>}
              </div>
              <div>
                <p className="font-semibold text-gray-600 mb-1">Palabras clave</p>
                {tags.length > 0
                  ? <div className="flex flex-wrap gap-1">{tags.map(t =>
                      <span key={t} className="px-1.5 py-0.5 rounded bg-gray-100 text-gray-600">{t}</span>
                    )}</div>
                  : <span className="text-gray-400">—</span>}
                <p className="text-gray-400 mt-1">Origen: {doc.fuenteIngesta === 'UPLOAD' ? 'Carga directa' : doc.rutaOriginal}</p>
              </div>
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

// ─── Página principal ──────────────────────────────────────────────────────────
export function BaseConocimiento() {
  const [docs, setDocs]         = useState<BaseConocimientoDto[]>([]);
  const [total, setTotal]       = useState(0);
  const [loading, setLoading]   = useState(false);
  const [busqueda, setBusqueda] = useState('');
  const [result, setResult]     = useState<IngestResultado | null>(null);

  // Panel ingestión directorio
  const [ruta, setRuta]           = useState('');
  const [procesandoDir, setPDir]  = useState(false);

  // Panel upload
  const [uploading, setUploading] = useState(false);
  const [dragging, setDragging]   = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const cargar = async (busq?: string) => {
    setLoading(true);
    try {
      const termino = busq ?? busqueda;
      const res = await baseConocimientoApi.listar({ busqueda: termino || undefined });
      setDocs(res.items);
      setTotal(res.total);
    } catch { toast.error('Error cargando base de conocimiento'); }
    finally { setLoading(false); }
  };

  useEffect(() => { cargar(); }, []);

  const handleBusqueda = (e: React.FormEvent) => {
    e.preventDefault();
    cargar(busqueda);
  };

  const handleIngestirDir = async () => {
    if (!ruta.trim()) { toast.error('Ingresa una ruta de directorio'); return; }
    setPDir(true);
    try {
      const r = await baseConocimientoApi.ingestirDirectorio(ruta.trim());
      setResult(r);
      toast.success(`Ingesta completada: ${r.procesados} archivos procesados`);
      cargar();
    } catch { toast.error('Error al procesar el directorio'); }
    finally { setPDir(false); }
  };

  const handleUpload = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    setUploading(true);
    try {
      const r = await baseConocimientoApi.upload(Array.from(files));
      setResult({ procesados: r.procesados, errores: r.errores, omitidos: 0, detalles: r.detalles });
      toast.success(`${r.procesados} archivo(s) indexado(s)`);
      cargar();
    } catch { toast.error('Error al subir archivos'); }
    finally { setUploading(false); }
  };

  const handleDelete = async (id: string) => {
    try {
      await baseConocimientoApi.eliminar(id);
      toast.success('Documento eliminado de la base de conocimiento');
      cargar();
    } catch { toast.error('Error al eliminar'); }
  };

  const DOMINIO_STATS = docs.reduce<Record<string, number>>((acc, d) => {
    const dom = d.dominioDetectado ?? 'Sin clasificar';
    acc[dom] = (acc[dom] ?? 0) + 1;
    return acc;
  }, {});

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
            <Database size={22} className="text-blue-600" />
            Base de Conocimiento
          </h1>
          <p className="text-sm text-gray-500 mt-0.5">
            Documentos de auditoría indexados — el Agente IA los usa automáticamente
          </p>
        </div>
        <button onClick={() => cargar()} className="flex items-center gap-1.5 text-xs text-gray-500 hover:text-gray-700 transition">
          <RefreshCw size={13} className={loading ? 'animate-spin' : ''} /> Actualizar
        </button>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4 text-center">
          <p className="text-xs text-gray-500">Documentos</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">{total}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4 text-center">
          <p className="text-xs text-gray-500">Palabras totales</p>
          <p className="text-2xl font-bold text-blue-700 mt-1">
            {docs.reduce((s, d) => s + d.totalPalabras, 0).toLocaleString()}
          </p>
        </div>
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4 text-center">
          <p className="text-xs text-gray-500">Dominios cubiertos</p>
          <p className="text-2xl font-bold text-green-700 mt-1">
            {Object.keys(DOMINIO_STATS).filter(d => d !== 'Sin clasificar').length}
          </p>
        </div>
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4 text-center">
          <p className="text-xs text-gray-500">Tipos de archivo</p>
          <p className="text-2xl font-bold text-gray-700 mt-1">
            {new Set(docs.map(d => d.tipoArchivo)).size}
          </p>
        </div>
      </div>

      {/* Paneles de ingesta */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Ingesta por directorio */}
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
          <div className="flex items-center gap-2 mb-3">
            <FolderOpen size={18} className="text-blue-600" />
            <h2 className="font-semibold text-gray-800 text-sm">Ingestar directorio del servidor</h2>
          </div>
          <p className="text-xs text-gray-500 mb-3">
            Indica la ruta de una carpeta accesible por el servidor. El sistema leerá todos los archivos
            (PDF, Word, Excel, CSV, TXT) y los indexará automáticamente.
          </p>
          <div className="flex gap-2">
            <input
              type="text"
              value={ruta}
              onChange={e => setRuta(e.target.value)}
              placeholder="Ej: /archivos/auditorias/2026"
              className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              onKeyDown={e => e.key === 'Enter' && handleIngestirDir()}
            />
            <button
              onClick={handleIngestirDir}
              disabled={procesandoDir}
              className="flex items-center gap-1.5 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition">
              {procesandoDir ? <RefreshCw size={14} className="animate-spin" /> : <FolderOpen size={14} />}
              {procesandoDir ? 'Procesando...' : 'Ingestar'}
            </button>
          </div>
          <p className="text-[11px] text-gray-400 mt-2">
            Soporta: .pdf · .docx · .xlsx · .csv · .txt · .md · Subdirectorios incluidos
          </p>
        </div>

        {/* Upload directo */}
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
          <div className="flex items-center gap-2 mb-3">
            <Upload size={18} className="text-purple-600" />
            <h2 className="font-semibold text-gray-800 text-sm">Subir archivos directamente</h2>
          </div>
          <p className="text-xs text-gray-500 mb-3">
            Arrastra o selecciona archivos de auditoría desde tu computadora. Múltiples archivos permitidos.
          </p>
          <div
            onDragOver={e => { e.preventDefault(); setDragging(true); }}
            onDragLeave={() => setDragging(false)}
            onDrop={e => { e.preventDefault(); setDragging(false); handleUpload(e.dataTransfer.files); }}
            onClick={() => inputRef.current?.click()}
            className={`border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition
              ${dragging ? 'border-purple-400 bg-purple-50' : 'border-gray-300 hover:border-purple-400 hover:bg-gray-50'}`}
          >
            <input ref={inputRef} type="file" multiple className="hidden"
              accept=".pdf,.docx,.doc,.xlsx,.xls,.csv,.txt,.md"
              onChange={e => handleUpload(e.target.files)} />
            {uploading
              ? <div className="flex items-center justify-center gap-2 text-purple-600">
                  <RefreshCw size={18} className="animate-spin" />
                  <span className="text-sm font-medium">Procesando archivos...</span>
                </div>
              : <>
                  <Upload size={28} className="mx-auto text-gray-400 mb-2" />
                  <p className="text-sm text-gray-600 font-medium">Arrastra archivos aquí</p>
                  <p className="text-xs text-gray-400 mt-1">o haz clic para seleccionar</p>
                </>}
          </div>
        </div>
      </div>

      {/* Resultado de ingesta */}
      {result && <ResultPanel r={result} onClose={() => setResult(null)} />}

      {/* Distribución por dominio */}
      {Object.keys(DOMINIO_STATS).length > 0 && (
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-4">
          <p className="text-xs font-semibold text-gray-500 mb-3 flex items-center gap-1">
            <BookOpen size={12} /> Documentos por dominio de auditoría
          </p>
          <div className="flex flex-wrap gap-2">
            {Object.entries(DOMINIO_STATS)
              .sort((a, b) => b[1] - a[1])
              .map(([dom, count]) => (
                <span key={dom} className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium bg-blue-50 text-blue-700 border border-blue-100">
                  {dom} <span className="bg-blue-600 text-white rounded-full w-4 h-4 flex items-center justify-center text-[10px]">{count}</span>
                </span>
              ))}
          </div>
        </div>
      )}

      {/* Búsqueda y tabla */}
      <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
        <div className="px-4 py-3 border-b border-gray-50 flex items-center gap-3">
          <form onSubmit={handleBusqueda} className="flex-1 flex items-center gap-2">
            <div className="relative flex-1 max-w-xs">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                value={busqueda}
                onChange={e => setBusqueda(e.target.value)}
                placeholder="Buscar en documentos..."
                className="w-full pl-8 pr-3 py-1.5 text-xs border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <button type="submit"
              className="px-3 py-1.5 bg-blue-600 text-white rounded-lg text-xs hover:bg-blue-700 transition">
              Buscar
            </button>
            {busqueda && (
              <button type="button" onClick={() => { setBusqueda(''); cargar(''); }}
                className="text-xs text-gray-500 hover:text-gray-700">
                Limpiar
              </button>
            )}
          </form>
          <span className="text-xs text-gray-400">{total} documentos</span>
        </div>

        {loading ? (
          <div className="py-16 text-center text-gray-400">
            <RefreshCw size={24} className="animate-spin mx-auto mb-2 text-gray-300" />
            Cargando documentos...
          </div>
        ) : docs.length === 0 ? (
          <div className="py-16 text-center text-gray-400">
            <Database size={36} className="mx-auto mb-3 text-gray-300" />
            <p className="font-medium">La base de conocimiento está vacía</p>
            <p className="text-sm mt-1">Ingesta un directorio o sube archivos para comenzar</p>
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-100">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase">Archivo</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase">Dominio</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase">Tipo</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase">Tamaño</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase">Palabras</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase">Indexado</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {docs.map(doc => (
                <DocRow key={doc.id} doc={doc} onDelete={handleDelete} />
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
