import api from './client';

export interface CargaResultado {
  totalRegistros: number;
  insertados: number;
  actualizados: number;
  errores: number;
  detalleErrores: string[];
}

export const cargasApi = {
  cargarEmpleados: (file: File, sociedadCodigo?: string): Promise<CargaResultado> => {
    const form = new FormData();
    form.append('archivo', file);
    if (sociedadCodigo) form.append('sociedadCodigo', sociedadCodigo);
    return api.post('/cargas/empleados', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },

  cargarUsuarios: (file: File, sistema: string, sociedadCodigo?: string): Promise<CargaResultado> => {
    const form = new FormData();
    form.append('archivo', file);
    form.append('sistema', sistema);
    if (sociedadCodigo) form.append('sociedadCodigo', sociedadCodigo);
    return api.post('/cargas/usuarios-sistema', form, {
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

  cargarRolesSAP: (file: File, sistema: string, sociedadCodigo?: string): Promise<CargaResultado> => {
    const form = new FormData();
    form.append('archivo', file);
    form.append('sistema', sistema);
    if (sociedadCodigo) form.append('sociedadCodigo', sociedadCodigo);
    return api.post('/cargas/sap-roles', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },

  descargarPlantillaSapRoles: async () => {
    const res = await api.get('/cargas/plantilla/sap-roles', { responseType: 'blob' });
    const url = URL.createObjectURL(res.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'plantilla_sap_roles.xlsx';
    a.click();
    URL.revokeObjectURL(url);
  },

  cargarMatrizPuestos: (file: File, sociedadCodigo?: string): Promise<CargaResultado> => {
    const form = new FormData();
    form.append('archivo', file);
    if (sociedadCodigo) form.append('sociedadCodigo', sociedadCodigo);
    return api.post('/cargas/matriz-puestos', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },

  descargarPlantillaMatrizPuestos: async () => {
    const res = await api.get('/cargas/plantilla/matriz-puestos', { responseType: 'blob' });
    const url = URL.createObjectURL(res.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'plantilla_matriz_puestos.xlsx';
    a.click();
    URL.revokeObjectURL(url);
  },

  cargarCasosSeSuite: (file: File, sociedadCodigo?: string): Promise<CargaResultado> => {
    const form = new FormData();
    form.append('archivo', file);
    if (sociedadCodigo) form.append('sociedadCodigo', sociedadCodigo);
    return api.post('/cargas/casos-sesuite', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },

  descargarPlantillaCasosSeSuite: async () => {
    const res = await api.get('/cargas/plantilla/casos-sesuite', { responseType: 'blob' });
    const url = URL.createObjectURL(res.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'plantilla_casos_sesuite.xlsx';
    a.click();
    URL.revokeObjectURL(url);
  },

  // ── Lotes de Carga ────────────────────────────────────────────────────────
  getLotes: (params?: { tipoCarga?: string; sociedadCodigo?: string; limit?: number }): Promise<LoteCargaDto[]> =>
    api.get('/cargas/lotes', { params }).then(r => r.data),

  // ── Visor Matriz de Puestos ───────────────────────────────────────────────
  getMatrizPuestos: (params: {
    usuario?: string; puesto?: string; rol?: string; transaccion?: string;
    page?: number; pageSize?: number;
  }): Promise<MatrizPuestosResultado> =>
    api.get('/cargas/matriz-puestos', { params }).then(r => r.data),

  // ── Snapshots Entra ID ────────────────────────────────────────────────────
  cargarSnapshotEntraID: (file: File, nombre?: string): Promise<SnapshotEntraIDResultado> => {
    const form = new FormData();
    form.append('archivo', file);
    if (nombre) form.append('nombre', nombre);
    return api.post('/cargas/snapshot-entraid', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },

  getSnapshotsEntraID: (): Promise<SnapshotEntraIDDto[]> =>
    api.get('/cargas/snapshots-entraid').then(r => r.data),

  syncEntraIDDirecto: (nombreInstantanea?: string): Promise<SnapshotEntraIDResultado> =>
    api.post('/cargas/snapshot-entraid/sync', { nombreInstantanea: nombreInstantanea ?? null })
      .then(r => r.data),

  descargarSnapshotEntraID: async (id: string, nombre: string) => {
    const res = await api.get(`/cargas/snapshot-entraid/${id}/excel`, { responseType: 'blob' });
    const url = URL.createObjectURL(res.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = `EntraID_Snapshot_${nombre}.xlsx`;
    a.click();
    URL.revokeObjectURL(url);
  },

  descargarPlantillaEntraID: async () => {
    const res = await api.get('/cargas/plantilla/snapshot-entraid', { responseType: 'blob' });
    const url = URL.createObjectURL(res.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'plantilla_entra_id.xlsx';
    a.click();
    URL.revokeObjectURL(url);
  },

  purgarCargasAntiguas: (): Promise<PurgarCargasResultado> =>
    api.post('/cargas/purgar-antiguas').then(r => r.data),
};

export interface LoteCargaDto {
  id: string;
  tipoCarga: string;
  fechaCarga: string;
  sociedadCodigo?: string;
  sociedadNombre?: string;
  nombreArchivo?: string;
  totalRegistros: number;
  insertados: number;
  actualizados: number;
  errores: number;
  cargadoPor?: string;
  esVigente: boolean;
}

export interface MatrizPuestoDto {
  usuarioSAP: string;
  nombreCompleto?: string;
  cedula?: string;
  sociedad?: string;
  departamento?: string;
  puesto?: string;
  email?: string;
  rol: string;
  transaccion?: string;
  inicioValidez?: string;
  finValidez?: string;
  ultimoIngreso?: string;
  fechaRevisionContraloria?: string;
}

export interface MatrizPuestosResultado {
  total: number;
  page: number;
  pageSize: number;
  items: MatrizPuestoDto[];
}

export interface SnapshotEntraIDResultado {
  snapshotId: string;
  nombre: string;
  fechaInstantanea: string;
  totalRegistros: number;
  errores: number;
  detalleErrores: string[];
  origen?: string;
}

export interface SnapshotEntraIDDto {
  id: string;
  nombre: string;
  fechaInstantanea: string;
  totalRegistros: number;
  creadoPor?: string;
  origen?: string;
}

export interface PurgarCargasResultado {
  lotesBorrados: number;
  registrosBorrados: number;
  detallesPorTipo: Record<string, number>;
}
