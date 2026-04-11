import api from './client';

export interface BaseConocimientoDto {
  id: string;
  nombreArchivo: string;
  rutaOriginal: string;
  tipoArchivo: string;
  tamanoBytes: number;
  totalPalabras: number;
  dominioDetectado: string | null;
  controlesDetectados: string | null;
  tags: string | null;
  estado: string;
  fuenteIngesta: string;
  ingresadoPor: string | null;
  creadoAt: string;
  resumen: string;
}

export interface IngestResultado {
  procesados: number;
  errores: number;
  omitidos: number;
  detalles: string[];
}

export const baseConocimientoApi = {
  listar: (params?: { dominio?: string; busqueda?: string; page?: number; pageSize?: number }) =>
    api.get<{ items: BaseConocimientoDto[]; total: number }>('/base-conocimiento', { params })
      .then(r => r.data),

  ingestirDirectorio: (rutaDirectorio: string) =>
    api.post<IngestResultado>('/base-conocimiento/ingestir-directorio', { rutaDirectorio })
      .then(r => r.data),

  upload: (files: File[]) => {
    const form = new FormData();
    files.forEach(f => form.append('files', f));
    return api.post<{ procesados: number; errores: number; detalles: string[] }>(
      '/base-conocimiento/upload', form,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    ).then(r => r.data);
  },

  eliminar: (id: string) =>
    api.delete(`/base-conocimiento/${id}`),

  buscar: (q: string, topK = 5) =>
    api.get<{ contexto: string }>('/base-conocimiento/buscar', { params: { q, topK } })
      .then(r => r.data),
};
