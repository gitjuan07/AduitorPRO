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
  configuracionJson?: string;
}

export interface TestConectorResult {
  exitoso: boolean;
  duracionMs: number;
  mensaje: string;
}

export interface EjecutarResult {
  exitoso: boolean;
  mensaje: string;
  duracionMs: number;
  totalFilas: number;
  columnas: string[];
  filas: (string | null)[][];
}

export const conectoresApi = {
  getAll: () => api.get<{ items: ConectorDto[] } | ConectorDto[]>('/conectores')
    .then(r => Array.isArray(r.data) ? r.data : (r.data as { items: ConectorDto[] }).items),

  getById: (id: string) => api.get<ConectorDto>(`/conectores/${id}`).then(r => r.data),

  crear: (data: Partial<ConectorDto>) =>
    api.post<ConectorDto>('/conectores', data).then(r => r.data),

  actualizar: (id: string, data: Partial<ConectorDto>) =>
    api.put<ConectorDto>(`/conectores/${id}`, data).then(r => r.data),

  eliminar: (id: string) => api.delete(`/conectores/${id}`),

  probar: (id: string) =>
    api.post<TestConectorResult>(`/conectores/${id}/probar`).then(r => r.data),

  ejecutar: (id: string, maxFilas = 500) =>
    api.post<EjecutarResult>(`/conectores/${id}/ejecutar`, null, { params: { maxFilas } }).then(r => r.data),

  probarQuery: (id: string, configuracionJsonOverride?: string) =>
    api.post<EjecutarResult>(`/conectores/${id}/probar-query`, { configuracionJsonOverride }).then(r => r.data),
};
