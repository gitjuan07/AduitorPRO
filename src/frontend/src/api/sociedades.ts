import api from './client';

export interface SociedadDto {
  id: number;
  codigo: string;
  nombre: string;
  pais?: string;
  activa: boolean;
}

export const sociedadesApi = {
  getAll: (soloActivas?: boolean) =>
    api.get<SociedadDto[]>('/sociedades', {
      params: soloActivas !== undefined ? { soloActivas } : {},
    }).then(r => r.data),

  getByCodigo: (codigo: string) =>
    api.get<SociedadDto>(`/sociedades/${codigo}`).then(r => r.data),
};
