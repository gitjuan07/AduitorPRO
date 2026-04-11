import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getSimulaciones, iniciarSimulacion, cancelarSimulacion, type SimulacionListDto } from '../api/simulaciones';
import { toast } from 'sonner';
import { PlayCircle, XCircle, ChevronRight } from 'lucide-react';
import { useForm } from 'react-hook-form';

const estadoColor: Record<string, string> = {
  COMPLETADA: 'bg-green-100 text-green-800',
  EN_PROCESO: 'bg-blue-100 text-blue-800',
  PENDIENTE: 'bg-yellow-100 text-yellow-800',
  ERROR: 'bg-red-100 text-red-800',
  CANCELADA: 'bg-gray-100 text-gray-700',
};

export function Simulaciones() {
  const [items, setItems] = useState<SimulacionListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const { register, handleSubmit, reset } = useForm();
  const navigate = useNavigate();

  const load = () => {
    getSimulaciones()
      .then((r) => setItems(r.items))
      .catch(() => toast.error('Error al cargar simulaciones'))
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const onSubmit = async (data: Record<string, unknown>) => {
    try {
      await iniciarSimulacion({
        nombre: data.nombre as string,
        tipo: 'MANUAL',
        sociedadIds: [1],
        periodoInicio: data.periodoInicio as string,
        periodoFin: data.periodoFin as string,
      });
      toast.success('Simulación iniciada');
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
              <label className="text-xs text-gray-500">Nombre</label>
              <input {...register('nombre', { required: true })} className="w-full border rounded-lg px-3 py-2 text-sm mt-1" />
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
              Iniciar
            </button>
            <button type="button" onClick={() => setShowForm(false)} className="border px-4 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-50">
              Cancelar
            </button>
          </div>
        </form>
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
                <tr key={s.id} className="hover:bg-gray-50 cursor-pointer" onClick={() => navigate(`/simulaciones/${s.id}`)}>
                  <td className="px-4 py-3 font-medium text-gray-800">{s.nombre}</td>
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
                      {(s.estado === 'PENDIENTE' || s.estado === 'EN_PROCESO') && (
                        <button onClick={e => { e.stopPropagation(); cancelar(s.id); }} className="text-red-400 hover:text-red-700">
                          <XCircle size={15} />
                        </button>
                      )}
                      <ChevronRight size={14} className="text-gray-300" />
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
