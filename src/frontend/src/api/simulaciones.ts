import api from './client';

export interface SimulacionListDto {
  id: string;
  nombre: string;
  estado: string;
  scoreMadurez?: number;
  porcentajeCumplimiento?: number;
  totalControles?: number;
  controlesRojo?: number;
  iniciadaPor: string;
  iniciadaAt: string;
  completadaAt?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface IniciarSimulacionRequest {
  nombre: string;
  descripcion?: string;
  tipo: 'MANUAL' | 'PROGRAMADA' | 'BAJO_DEMANDA';
  sociedadIds: number[];
  periodoInicio: string;
  periodoFin: string;
  dominioIds?: number[];
}

export interface ResultadoControlDto {
  id: string;
  codigoControl: string;
  nombreControl: string;
  dominio: string;
  semaforo: string;
  criticidad: string;
  resultadoDetalle?: string;
  analisisIa?: string;
  recomendacion?: string;
}

export interface SimulacionDetalleDto extends SimulacionListDto {
  descripcion?: string;
  periodoInicio: string;
  periodoFin: string;
  controlesVerde?: number;
  controlesAmarillo?: number;
  resultados: ResultadoControlDto[];
}

export const getSimulaciones = (page = 1, pageSize = 20): Promise<PagedResult<SimulacionListDto>> =>
  api.get('/simulaciones', { params: { page, pageSize } }).then((r) => r.data);

export const getSimulacion = (id: string): Promise<SimulacionDetalleDto> =>
  api.get(`/simulaciones/${id}`).then((r) => r.data);

export const iniciarSimulacion = (data: IniciarSimulacionRequest): Promise<{ id: string }> =>
  api.post('/simulaciones', data).then((r) => r.data);

export const cancelarSimulacion = (id: string) =>
  api.post(`/simulaciones/${id}/cancelar`);

export interface ControlCruzadoRequest {
  objetivo?: string;
  tipoControlCruzado?: 'COMPLETO' | 'SAP_NOMINA' | 'SAP_ENTRA_ID' | 'SOD_ONLY';
}

export interface ControlCruzadoResultado {
  totalHallazgos: number;
  criticos: number;
  medios: number;
  bajos: number;
  porRegla: Record<string, number>;
}

export const ejecutarControlCruzado = (
  id: string,
  data: ControlCruzadoRequest
): Promise<ControlCruzadoResultado> =>
  api.post(`/simulaciones/${id}/ejecutar-control-cruzado`, data).then((r) => r.data);

export const borrarTodasSimulaciones = (): Promise<{ borradas: number }> =>
  api.delete('/simulaciones/todas').then((r) => r.data);
