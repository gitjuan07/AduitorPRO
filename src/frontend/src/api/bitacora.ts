import api from './client';

export interface BitacoraEventoDto {
  id: string;
  usuarioId: string;
  usuarioEmail?: string;
  accion: string;
  recurso?: string;
  recursoId?: string;
  descripcion?: string;
  datosAntes?: string;
  datosDespues?: string;
  ipOrigen?: string;
  exitoso: boolean;
  ocurridoAt: string;
}

export interface BitacoraResponse {
  items: BitacoraEventoDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const bitacoraApi = {
  getAll: (params?: {
    usuarioId?: string;
    accion?: string;
    recurso?: string;
    desde?: string;
    hasta?: string;
    page?: number;
    pageSize?: number;
  }) => api.get<BitacoraResponse>('/bitacora', { params }).then(r => r.data),
};
