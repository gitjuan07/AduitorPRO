import api from './client';

export interface EvidenciaDto {
  id: string;
  nombreArchivo: string;
  descripcion?: string;
  contentType?: string;
  tamanoBytes: number;
  blobUrl: string;
  tipoEvidencia: string;
  hallazgoId?: string;
  simulacionId?: string;
  subidoPor?: string;
  subidoAt: string;
  sasUrl?: string;
}

export interface EvidenciasResponse {
  items: EvidenciaDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const evidenciasApi = {
  getAll: (params?: {
    hallazgoId?: string;
    simulacionId?: string;
    tipo?: string;
    page?: number;
    pageSize?: number;
  }) => api.get<EvidenciasResponse>('/evidencias', { params }).then(r => r.data),

  upload: (formData: FormData) =>
    api.post<EvidenciaDto>('/evidencias/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data),

  delete: (id: string) => api.delete(`/evidencias/${id}`),
};
