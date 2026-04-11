import api from './client';

export interface ConectorDto {
  id: string;
  nombre: string;
  sistema: string;
  tipoConexion: string;
  estado: string;
  activo: boolean;
  ultimoTest?: string;
  ultimoTestResultado?: string;
  descripcion?: string;
}

export interface TestConectorResult {
  exitoso: boolean;
  duracionMs: number;
  mensaje: string;
}

export const conectoresApi = {
  getAll: () => api.get<ConectorDto[]>('/conectores').then(r => r.data),

  getById: (id: string) => api.get<ConectorDto>(`/conectores/${id}`).then(r => r.data),

  crear: (data: Partial<ConectorDto>) =>
    api.post<ConectorDto>('/conectores', data).then(r => r.data),

  actualizar: (id: string, data: Partial<ConectorDto>) =>
    api.put<ConectorDto>(`/conectores/${id}`, data).then(r => r.data),

  eliminar: (id: string) => api.delete(`/conectores/${id}`),

  probar: (id: string) =>
    api.post<TestConectorResult>(`/conectores/${id}/probar`).then(r => r.data),

  ejecutar: (id: string) =>
    api.post(`/conectores/${id}/ejecutar`),
};
