import { useState, useEffect, useRef } from 'react';
import { evidenciasApi, type EvidenciaDto } from '../api/evidencias';
import { toast } from 'sonner';
import { Upload, FileText, Image, FileSpreadsheet, Trash2, Download, Search } from 'lucide-react';

const TIPO_ICONS: Record<string, React.ReactNode> = {
  ARCHIVO: <FileText size={16} />,
  CAPTURA_PANTALLA: <Image size={16} />,
  REPORTE_SISTEMA: <FileSpreadsheet size={16} />,
  CORREO: <FileText size={16} />,
  DOCUMENTO_FIRMADO: <FileText size={16} />,
};

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1048576).toFixed(1)} MB`;
}

export function Evidencias() {
  const [items, setItems] = useState<EvidenciaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const load = () => {
    setLoading(true);
    evidenciasApi.getAll({ pageSize: 100 })
      .then(r => setItems(r.items))
      .catch(() => toast.error('Error al cargar evidencias'))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const handleUpload = async (files: FileList) => {
    if (!files.length) return;
    setUploading(true);
    try {
      for (const file of Array.from(files)) {
        const fd = new FormData();
        fd.append('archivo', file);
        fd.append('tipoEvidencia', 'ARCHIVO');
        await evidenciasApi.upload(fd);
      }
      toast.success(`${files.length} evidencia(s) cargada(s) correctamente`);
      load();
    } catch {
      toast.error('Error al cargar evidencias');
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('¿Eliminar esta evidencia?')) return;
    try {
      await evidenciasApi.delete(id);
      toast.success('Evidencia eliminada');
      setItems(prev => prev.filter(e => e.id !== id));
    } catch {
      toast.error('Error al eliminar la evidencia');
    }
  };

  const filtered = items.filter(e =>
    !search || e.nombreArchivo.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Evidencias</h1>
          <p className="text-sm text-gray-500 mt-0.5">Gestión documental de respaldo de controles</p>
        </div>
        <button
          onClick={() => fileInputRef.current?.click()}
          disabled={uploading}
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition text-sm font-medium"
        >
          <Upload size={16} />
          {uploading ? 'Cargando...' : 'Cargar evidencia'}
        </button>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept=".pdf,.docx,.xlsx,.csv,.png,.jpg,.jpeg"
          className="hidden"
          onChange={e => e.target.files && handleUpload(e.target.files)}
        />
      </div>

      <div className="relative mb-4">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
        <input
          type="text"
          placeholder="Buscar por nombre de archivo..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Drop zone */}
      <div
        className="border-2 border-dashed border-gray-300 rounded-xl p-8 text-center mb-6 hover:border-blue-400 transition cursor-pointer"
        onClick={() => fileInputRef.current?.click()}
        onDragOver={e => e.preventDefault()}
        onDrop={e => { e.preventDefault(); e.dataTransfer.files && handleUpload(e.dataTransfer.files); }}
      >
        <Upload size={32} className="mx-auto text-gray-400 mb-2" />
        <p className="text-gray-500 text-sm">
          Arrastra archivos aquí o <span className="text-blue-600 font-medium">haz clic para seleccionar</span>
        </p>
        <p className="text-xs text-gray-400 mt-1">PDF, Word, Excel, CSV, PNG, JPG (máx. 50 MB)</p>
      </div>

      {/* Estadísticas */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        {[
          { label: 'Total',     value: items.length,                                              color: 'text-gray-700' },
          { label: 'Archivos',  value: items.filter(e => e.tipoEvidencia === 'ARCHIVO').length,   color: 'text-blue-600' },
          { label: 'Capturas',  value: items.filter(e => e.tipoEvidencia === 'CAPTURA_PANTALLA').length, color: 'text-purple-600' },
          { label: 'Reportes',  value: items.filter(e => e.tipoEvidencia === 'REPORTE_SISTEMA').length,  color: 'text-green-600' },
        ].map(s => (
          <div key={s.label} className="bg-white rounded-xl p-4 shadow-sm border border-gray-100">
            <p className="text-xs text-gray-500">{s.label}</p>
            <p className={`text-2xl font-bold mt-1 ${s.color}`}>{s.value}</p>
          </div>
        ))}
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Cargando evidencias...</div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border border-gray-100">
          <FileText size={40} className="mx-auto text-gray-300 mb-3" />
          <p className="text-gray-500 font-medium">
            {search ? 'No se encontraron evidencias con ese nombre' : 'Aún no hay evidencias cargadas'}
          </p>
          {!search && <p className="text-sm text-gray-400 mt-1">Carga la primera evidencia usando el botón de arriba</p>}
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-100">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Archivo</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Tipo</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Tamaño</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Subido por</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Fecha</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {filtered.map(ev => (
                <tr key={ev.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <span className="text-gray-400">{TIPO_ICONS[ev.tipoEvidencia] ?? <FileText size={16} />}</span>
                      <span className="font-medium text-gray-800 truncate max-w-xs">{ev.nombreArchivo}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="px-2 py-0.5 bg-blue-50 text-blue-700 rounded text-xs font-medium">{ev.tipoEvidencia}</span>
                  </td>
                  <td className="px-4 py-3 text-gray-500">{formatBytes(ev.tamanoBytes)}</td>
                  <td className="px-4 py-3 text-gray-500 truncate max-w-[180px]">{ev.subidoPor ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-500">{new Date(ev.subidoAt).toLocaleDateString('es-CR')}</td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2 justify-end">
                      {ev.sasUrl && (
                        <a href={ev.sasUrl} target="_blank" rel="noopener noreferrer"
                          className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded transition" title="Descargar">
                          <Download size={15} />
                        </a>
                      )}
                      <button onClick={() => handleDelete(ev.id)}
                        className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition" title="Eliminar">
                        <Trash2 size={15} />
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
