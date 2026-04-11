import api from './client';

export interface PlanAccionDto {
  hallazgoId: string;
  titulo: string;
  criticidad: string;
  dominio?: string;
  estado: string;
  planAccion?: string;
  responsable?: string;
  fechaCompromiso?: string;
  fechaCierre?: string;
  esVencido: boolean;
  diasRestantes: number;
}

export interface PlanesAccionResponse {
  items: PlanAccionDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const planesAccionApi = {
  getAll: (params?: {
    estado?: string;
    responsable?: string;
    vencidos?: boolean;
    page?: number;
    pageSize?: number;
  }) => api.get<PlanesAccionResponse>('/planes-accion', { params }).then(r => r.data),

  crear: (data: {
    hallazgoId: string;
    descripcion: string;
    responsable: string;
    fechaCompromiso: string;
    recursos?: string;
  }) => api.post<string>('/planes-accion', data).then(r => r.data),

  actualizarEstatus: (data: {
    hallazgoId: string;
    nuevoEstado: string;
    comentario?: string;
  }) => api.put('/planes-accion/estatus', data),
};
