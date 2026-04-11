import api from './client';

export interface CargaResultado {
  totalRegistros: number;
  insertados: number;
  actualizados: number;
  errores: number;
  detalleErrores: string[];
}

export const cargasApi = {
  cargarEmpleados: (file: File, sociedadId: number): Promise<CargaResultado> => {
    const form = new FormData();
    form.append('file', file);
    form.append('sociedadId', String(sociedadId));
    return api.post('/cargas/empleados', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },

  cargarUsuarios: (file: File, sociedadId: number): Promise<CargaResultado> => {
    const form = new FormData();
    form.append('file', file);
    form.append('sociedadId', String(sociedadId));
    return api.post('/cargas/usuarios', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },

  descargarPlantillaEmpleados: async () => {
    const res = await api.get('/cargas/plantilla/empleados', { responseType: 'blob' });
    const url = URL.createObjectURL(res.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'plantilla_empleados.xlsx';
    a.click();
    URL.revokeObjectURL(url);
  },

  descargarPlantillaUsuarios: async () => {
    const res = await api.get('/cargas/plantilla/usuarios', { responseType: 'blob' });
    const url = URL.createObjectURL(res.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'plantilla_usuarios.xlsx';
    a.click();
    URL.revokeObjectURL(url);
  },
};
