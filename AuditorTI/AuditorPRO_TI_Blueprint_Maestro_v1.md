# AuditorPRO TI — Blueprint Maestro v2.0
### Plataforma Empresarial de Auditoría Preventiva Inteligente con Motor de Control Cruzado de Accesos
**Corporación ILG Logistics — Área de TI y Auditoría Interna**
**Versión:** 2.0 | **Fecha:** Abril 2026 | **Clasificación:** Confidencial — Uso interno

> **Cambios v2.0:** Se incorpora el **Motor de Control Cruzado de Accesos** — cruce tridimensional entre SAP, Evolution (Nómina) y Microsoft Entra ID usando la Cédula de Identidad como clave maestra. Se añade la **Matriz de Puestos** como fuente oficial de referencia de Contraloría. Las **Simulaciones** pasan a ser el centro de inteligencia de la plataforma, con dashboard de resultados con gráficos dinámicos, hallazgos automáticos, planes de acción sugeridos y generación masiva de evidencias para auditores. El **Agente IA** consume la base de conocimiento enriquecida con el universo completo de accesos para responder cualquier consulta de auditoría con precisión forense.

---

> **Principio rector:** El objetivo estratégico es **cero hallazgos de auditoría**. Cada funcionalidad, control, regla y flujo de esta plataforma existe para detectar debilidades antes que el auditor externo, y corregirlas con evidencia trazable y verificable.

---

## ÍNDICE MAESTRO

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Visión del Producto](#2-visión-del-producto)
3. [Arquitectura General de la Solución](#3-arquitectura-general-de-la-solución)
4. [Stack Tecnológico](#4-stack-tecnológico)
5. [Autenticación y Seguridad — Microsoft Entra ID](#5-autenticación-y-seguridad)
6. [Modelo de Datos Completo](#6-modelo-de-datos-completo)
7. [Módulos Funcionales — Especificación Detallada](#7-módulos-funcionales)
8. [Motor de Reglas de Auditoría](#8-motor-de-reglas-de-auditoría)
9. [Motor de Control Cruzado de Accesos](#9-motor-de-control-cruzado-de-accesos) ⭐ NUEVO v2.0
10. [Agente IA Auditor Preventivo](#10-agente-ia-auditor-preventivo)
11. [Motor de Integraciones — SOA Manager](#11-motor-de-integraciones)
12. [UX/UI — Diseño Fiori Enterprise](#12-uxui-diseño-fiori-enterprise)
13. [Trazabilidad y Bitácora de Auditoría](#13-trazabilidad-y-bitácora)
14. [Generación Automática de Entregables](#14-generación-de-entregables)
15. [API Interna — Especificación REST](#15-api-interna)
16. [Escenarios de Prueba — Matriz QA Completa](#16-escenarios-de-prueba-qa)
17. [Plan de Implementación y Roadmap](#17-roadmap-de-implementación)
18. [Alineación con Marcos Normativos](#18-alineación-normativa)
19. [Gestión de Configuración y Ambientes](#19-gestión-de-configuración)
20. [Arquitectura de Despliegue Azure](#20-arquitectura-de-despliegue-azure)
21. [Checklist de Verificación Final](#21-checklist-de-verificación-final)
22. [Base de Conocimiento — RAG Local](#22-base-de-conocimiento-rag-local)
23. [Simulaciones — Motor de Inteligencia Central](#23-simulaciones-motor-de-inteligencia-central) ⭐ NUEVO v2.0
24. [Dashboard de Resultados de Simulación](#24-dashboard-de-resultados-de-simulación) ⭐ NUEVO v2.0
25. [Control de Versiones del Blueprint](#25-control-de-versiones)

---

## 1. RESUMEN EJECUTIVO

### ¿Qué es AuditorPRO TI?

AuditorPRO TI es una **plataforma empresarial de auditoría preventiva inteligente** diseñada para actuar como un auditor interno digital disponible 24/7. Su propósito es anticipar hallazgos, evaluar controles, consolidar evidencias y guiar a los responsables hacia el cumplimiento antes de que llegue la auditoría formal.

### Problema que resuelve

| Situación actual | Con AuditorPRO TI |
|---|---|
| Hallazgos descubiertos durante auditoría formal | Detectados semanas o meses antes |
| Evidencias buscadas en el último momento | Organizadas, indexadas y listas |
| Sin correlación entre SAP, Nómina y Entra ID | Control cruzado automático tridimensional |
| Roles SAP sin justificación documentada | Cruzado contra Casos SE Suite automáticamente |
| Validación manual de Segregación de Funciones | Motor SoD automático con semáforos por riesgo |
| Políticas inconsistentes o desactualizadas | Revisadas y fortalecidas continuamente |
| Reportes preparados manualmente | Generados en segundos con gráficos ejecutivos |
| Sin visibilidad del estado de madurez | Dashboard ejecutivo en tiempo real |

### La cédula de identidad como clave maestra de auditoría

> **Principio fundamental de v2.0:** La **Cédula de Identidad (ID)** es el único identificador confiable que atraviesa los tres sistemas — SAP tiene el `ID` del empleado, Evolution tiene la nómina por cédula, Microsoft Entra ID contiene el perfil corporativo vinculado a la cédula. Cualquier discrepancia en este triángulo es un hallazgo potencial.

```
Cédula de Identidad (ID)
        │
        ├──▶ SAP: ¿Tiene usuario activo? ¿Qué roles/transacciones?
        │
        ├──▶ Evolution (Nómina): ¿Está activo laboralmente? ¿Cuál es su puesto?
        │
        └──▶ Microsoft Entra ID: ¿Tiene cuenta corporativa activa?

Si cualquiera de estos tres ejes no coincide → HALLAZGO AUTOMÁTICO
```

### Valor diferencial

- **Control cruzado tridimensional** SAP ↔ Evolution ↔ Entra ID por cédula
- **Matriz de Puestos** como referencia oficial aprobada por Contraloría
- **Validación SoD automática** con cruce contra Matriz de Segregación de Funciones
- **SE Suite como justificante** — si un rol extra tiene caso aprobado, no es hallazgo
- **Simulaciones con dashboard de resultados** descargable a Excel
- **Generación masiva de evidencias** para auditores desde hallazgos
- **Agente IA con contexto de accesos real** para consultas ad-hoc precisas

---

## 2. VISIÓN DEL PRODUCTO

### Nombre del sistema
**AuditorPRO TI** — Plataforma de Auditoría Preventiva Inteligente con Motor de Control Cruzado

### Tagline
> *"Encuentra las debilidades antes que el auditor. Llega a cero hallazgos."*

### Usuarios objetivo

| Rol | Uso principal |
|---|---|
| Administrador de TI (SAP Basis/Seguridad) | Recertificaciones SAP, control de accesos, SoD |
| Auditor Interno | Simulaciones, revisión de hallazgos, exportar evidencias |
| Gerente de TI | Dashboard ejecutivo, KPIs, calificación de madurez |
| Controlador Financiero | Validación SoD, riesgos de acceso, control de segregación |
| Responsable de proceso | Revisión de controles propios, planes de acción |
| Administrador de la plataforma | Conectores, reglas, cargas de datos |

---

## 3. ARQUITECTURA GENERAL DE LA SOLUCIÓN

### Diagrama de arquitectura v2.0

```
╔══════════════════════════════════════════════════════════════════════════════╗
║                      CAPA DE PRESENTACIÓN (Frontend React)                   ║
║  Dashboard · Simulaciones + Resultados · Hallazgos · Evidencias · Agente IA  ║
║  Cargas Masivas · Conectores SOA · Políticas · Bitácora · Base Conocimiento  ║
╚══════════════════════════════════════════════════════════════════════════════╝
                                 │ HTTPS / JWT Bearer
╔══════════════════════════════════════════════════════════════════════════════╗
║                    CAPA DE API (.NET 10 — Clean Architecture + CQRS)         ║
║  Controllers → MediatR Commands/Queries → Domain → Infrastructure           ║
║  Middleware: Auth · AuditLog · ValidationPipeline · RateLimiting            ║
╚══════════════════════════════════════════════════════════════════════════════╝
          │              │               │               │              │
          ▼              ▼               ▼               ▼              ▼
  ╔═══════════╗  ╔══════════════╗  ╔═══════════╗  ╔══════════╗  ╔══════════════╗
  ║ Azure SQL ║  ║ Azure Blob   ║  ║ Azure     ║  ║ Azure    ║  ║ Motor Cruce  ║
  ║ Database  ║  ║ Storage      ║  ║ OpenAI    ║  ║ Key Vault║  ║ de Accesos   ║
  ║ (datos    ║  ║ (evidencias, ║  ║ GPT-4o    ║  ║ (secrets)║  ║ SAP+EVL+AAD  ║
  ║ maestros) ║  ║ casos SE     ║  ║ (agente)  ║  ║          ║  ║ por cédula   ║
  ╚═══════════╝  ╚══════════════╝  ╚═══════════╝  ╚══════════╝  ╚══════════════╝
          │
╔══════════════════════════════════════════════════════════════════════════════╗
║              UNIVERSO DE DATOS — Fuentes para Control Cruzado                ║
║  [SAP Roles]  [Empleados Nómina]  [Matriz de Puestos]  [Casos SE Suite]     ║
║  [EntraID via Graph API]  [Matriz SoD]  [Usuarios Sistema AD]               ║
╚══════════════════════════════════════════════════════════════════════════════╝
```

---

## 4. STACK TECNOLÓGICO

| Capa | Componente | Tecnología | Versión |
|---|---|---|---|
| Frontend | Framework | React + TypeScript | 18.x |
| Frontend | Estilos | Tailwind CSS | 3.x |
| Frontend | Gráficos | **Recharts** | 2.x |
| Frontend | Tablas | TanStack Table | 8.x |
| Frontend | HTTP | Axios + interceptores | 1.x |
| Frontend | Auth | MSAL Browser (PKCE) | 3.x |
| Frontend | Notificaciones | Sonner | 1.x |
| Frontend | Build | Vite | 5.x |
| Backend | Framework | **.NET 10** (C#) | 10.0 |
| Backend | Arquitectura | Clean Architecture + MediatR | — |
| Backend | ORM | Entity Framework Core | 9.x |
| Backend | Validación | FluentValidation | 11.x |
| Backend | Excel | ClosedXML | 0.102 |
| Backend | Auth | Microsoft.Identity.Web | 3.x |
| Backend | Logging | Serilog → App Insights | — |
| Infra | DB | Azure SQL (sql-trackdocs-ilg) | Gen5 |
| Infra | Storage | Azure Blob (zaudstauditorpro) | GRS |
| Infra | AI | Azure OpenAI GPT-4o (Z-AUD-OAI) | — |
| Infra | Secrets | Azure Key Vault (Z-AUD-KV) | — |
| Infra | Backend | Azure App Service (Z-AUD-APP) | Linux .NET 10 |
| Infra | Frontend | Azure Static Web Apps (Z-AUD-SWA) | — |

---

## 5. AUTENTICACIÓN Y SEGURIDAD

### Flujo de autenticación
```
Usuario → MSAL Browser (PKCE) → Microsoft Entra ID → JWT Bearer Token
JWT contiene: userId, email, displayName, grupos de seguridad
Backend valida: firma, audiencia (api://clientId), expiración
Scope requerido: api://{clientId}/Simulaciones.Read (mínimo)
```

### Modelo de roles RBAC

| Rol | Descripción | Permisos |
|---|---|---|
| `AuditorPRO.Admin` | Administrador total | Todo, incluyendo conectores, reglas y cargas |
| `AuditorPRO.Auditor` | Auditor interno/externo | Simulaciones, hallazgos, evidencias, exportar |
| `AuditorPRO.TI.Senior` | Administrador TI senior | Simulaciones propias, hallazgos, planes de acción |
| `AuditorPRO.TI.Viewer` | Solo lectura TI | Dashboard, hallazgos, evidencias. Sin modificar |
| `AuditorPRO.Gerente` | Gerente/Controlador | Dashboard ejecutivo, KPIs, exportar reportes |

---

## 6. MODELO DE DATOS COMPLETO

### Convenciones
- PKs: GUID (uniqueidentifier) excepto catálogos (int identity)
- Todas las tablas operativas: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `IsDeleted` (soft delete)
- **La cédula de identidad (columna `Id` o `Cedula`) es la clave de cruce entre sistemas**

---

### BLOQUE 1: Maestros Corporativos

```sql
-- Sociedades, Departamentos, Puestos (catálogos de referencia)
-- Nómina Evolution: EmpleadosMaestro (NumeroEmpleado, Cedula=ID, NombreCompleto, Puesto, Estado)
-- Cédula = campo ID del export SAP = campo identificador en EntraID
```

**Tabla EmpleadosMaestro** (fuente: nómina Evolution)
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | Identificador interno |
| `Cedula` | NVARCHAR(20) | **Cédula de identidad — clave de cruce tridimensional** |
| `NumeroEmpleado` | NVARCHAR(50) | Código interno nómina |
| `NombreCompleto` | NVARCHAR(200) | Nombre completo |
| `CorreoCorporativo` | NVARCHAR(200) | Email corporativo |
| `EntraIdObject` | NVARCHAR(100) | Object ID en Microsoft Entra ID |
| `SociedadId` | INT | FK a Sociedades |
| `DepartamentoId` | INT | FK a Departamentos |
| `PuestoId` | INT | FK a Puestos |
| `EstadoLaboral` | ENUM | ACTIVO / INACTIVO / BAJA_PROCESADA |
| `FechaIngreso` | DATE | — |
| `FechaBaja` | DATE | — |

---

### BLOQUE 2: Accesos SAP (fuente: V_SAP_USR_RECERTIFICAION)

**Tabla UsuariosSistema** (usuarios de cualquier sistema: SAP, AD, Evolution, SE Suite)
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | — |
| `Sistema` | NVARCHAR(50) | SAP \| EVOLUTION \| AD \| SE_SUITE |
| `NombreUsuario` | NVARCHAR(100) | ID del usuario en el sistema (ej: JSERRANO) |
| `Cedula` | NVARCHAR(20) | **Cédula — clave de cruce con Nómina y EntraID** |
| `NombreCompleto` | NVARCHAR(200) | Del reporte SAP |
| `Sociedad` | NVARCHAR(50) | Del reporte SAP (ej: ILG-CR) |
| `Departamento` | NVARCHAR(100) | Del reporte SAP |
| `Puesto` | NVARCHAR(100) | Del reporte SAP |
| `Email` | NVARCHAR(200) | — |
| `Estado` | ENUM | ACTIVO \| BLOQUEADO \| INACTIVO |
| `TipoUsuario` | NVARCHAR(50) | DIALOG \| SERVICE \| BATCH |
| `UltimoIngreso` | DATETIME | Del campo ULTIMO_INGRESO del reporte SAP |
| `EmpleadoId` | GUID? | FK a EmpleadosMaestro (enlace por cédula) |

**Tabla RolesSistema**
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | — |
| `Sistema` | NVARCHAR(50) | SAP \| SE_SUITE \| otros |
| `NombreRol` | NVARCHAR(200) | Nombre técnico del rol (ej: Z_FI_CUENTAS_PAGAR) |
| `Descripcion` | NVARCHAR(500) | — |
| `NivelRiesgo` | NVARCHAR(20) | ALTO \| MEDIO \| BAJO |
| `EsCritico` | BIT | Si forma parte de SoD crítico |
| `TransaccionesAutorizadas` | NVARCHAR(MAX) | Tcodes separados por coma: FB60,FB65,F-43 |

**Tabla AsignacionesRolesUsuarios** (vínculo usuario ↔ rol)
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | — |
| `UsuarioId` | GUID | FK a UsuariosSistema |
| `RolId` | GUID | FK a RolesSistema |
| `FechaAsignacion` | DATE | INICIO_VALIDEZ del reporte SAP |
| `FechaVencimiento` | DATE | FIN_VALIDEZ del reporte SAP |
| `CasoSESuiteRef` | NVARCHAR(100) | Número de caso SE Suite que justifica el acceso |
| `Activa` | BIT | — |

---

### BLOQUE 3: Matriz de Puestos (fuente: aprobada por Contraloría)

> La Matriz de Puestos es la **referencia oficial** de qué roles SAP debería tener cada puesto. Es el estándar contra el que se compara el estado real de SAP para detectar excesos o deficiencias.

**Tabla MatrizPuestosRol** — _misma estructura que el export SAP con campo adicional_
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | — |
| `UsuarioSAP` | NVARCHAR(100) | Usuario SAP de referencia del puesto |
| `NombreCompleto` | NVARCHAR(200) | — |
| `Sociedad` | NVARCHAR(50) | — |
| `Departamento` | NVARCHAR(100) | — |
| `Puesto` | NVARCHAR(100) | **El puesto es la clave de referencia** |
| `Email` | NVARCHAR(200) | — |
| `Rol` | NVARCHAR(200) | Rol que debería tener el puesto |
| `InicioValidez` | DATE | Desde cuándo aplica según Contraloría |
| `FinValidez` | DATE | — |
| `Transaccion` | NVARCHAR(100) | Transacción autorizada para el puesto |
| `UltimoIngreso` | DATETIME | — |
| `FechaRevisionContraloria` | DATE | **31/07/2025** — fecha de aprobación Contraloría |
| `LoteCargaId` | GUID | FK al lote de carga que originó el registro |

---

### BLOQUE 4: Casos SE Suite (fuente: reporte de casos)

> Los casos SE Suite son la **justificación documental** para roles o transacciones SAP que excedan la Matriz de Puestos. Sin caso aprobado = hallazgo confirmado. Con caso = acceso justificado.

**Tabla CasosSESuite**
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | — |
| `NumeroCaso` | NVARCHAR(50) | **Número único del caso en SE Suite** |
| `Titulo` | NVARCHAR(500) | Descripción del caso |
| `UsuarioSAP` | NVARCHAR(100) | Usuario beneficiado por el caso |
| `Cedula` | NVARCHAR(20) | Cédula del empleado |
| `RolJustificado` | NVARCHAR(200) | Rol SAP que el caso justifica |
| `TransaccionesJustificadas` | NVARCHAR(MAX) | Tcodes justificados por el caso |
| `FechaAprobacion` | DATE | — |
| `FechaVencimiento` | DATE | — |
| `EstadoCaso` | NVARCHAR(50) | APROBADO \| PENDIENTE \| RECHAZADO \| VENCIDO |
| `Aprobador` | NVARCHAR(200) | — |
| `ArchivoAdjuntoUrl` | NVARCHAR(500) | URL en Azure Blob |
| `SimulacionId` | GUID? | FK a simulación si fue creado desde hallazgo |

---

### BLOQUE 5: Simulaciones de Auditoría (motor de inteligencia)

**Tabla SimulacionesAuditoria**
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | — |
| `Nombre` | NVARCHAR(300) | Nombre descriptivo de la simulación |
| `Objetivo` | NVARCHAR(2000) | **Descripción detallada de qué busca esta simulación** |
| `TipoSimulacion` | ENUM | ACCESOS_SAP \| CRUCE_NOMINA \| SOD \| COMPLETO \| PERSONALIZADO |
| `Estado` | ENUM | BORRADOR \| EN_EJECUCION \| COMPLETADA \| CON_HALLAZGOS \| CERRADA |
| `FechaInicio` | DATETIME | — |
| `FechaFin` | DATETIME | — |
| `SociedadId` | INT | FK a Sociedades |
| `FuentesDatosJson` | NVARCHAR(MAX) | **JSON con los lotes de carga usados y su descripción** |
| `ParametrosJson` | NVARCHAR(MAX) | Parámetros de ejecución |
| `ResumenResultados` | NVARCHAR(MAX) | JSON con métricas del dashboard |
| `TotalAnalizado` | INT | Total de registros procesados |
| `TotalHallazgos` | INT | Hallazgos generados por esta simulación |
| `TotalCriticos` | INT | Hallazgos de riesgo ALTO/CRITICO |
| `CreadoPor` | NVARCHAR(200) | — |

**Tabla FuentesDatosSimulacion** (detalle de archivos usados en cada simulación)
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | — |
| `SimulacionId` | GUID | FK a SimulacionesAuditoria |
| `TipoFuente` | ENUM | SAP_ROLES \| NOMINA \| MATRIZ_PUESTOS \| CASOS_SESUITE \| ENTRAID \| MATRIZ_SOD |
| `NombreArchivo` | NVARCHAR(300) | Nombre del archivo cargado |
| `Descripcion` | NVARCHAR(1000) | **Descripción de qué contiene este archivo** |
| `FechaCarga` | DATETIME | — |
| `TotalRegistros` | INT | Registros en el archivo |
| `LoteCargaId` | GUID? | Referencia al lote de carga |

---

### BLOQUE 6: Hallazgos y Evidencias

**Tabla Hallazgos**
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | — |
| `SimulacionId` | GUID | FK a SimulacionesAuditoria |
| `Codigo` | NVARCHAR(50) | Ej: HAL-2026-001 |
| `Titulo` | NVARCHAR(500) | — |
| `Descripcion` | NVARCHAR(MAX) | Detalle completo del hallazgo |
| `TipoHallazgo` | ENUM | ACCESO_INDEBIDO \| SOD_CONFLICTO \| USUARIO_INACTIVO_CON_ACCESO \| ROL_SIN_JUSTIFICACION \| DISCREPANCIA_NOMINA \| DISCREPANCIA_ENTRAID |
| `NivelRiesgo` | ENUM | CRITICO \| ALTO \| MEDIO \| BAJO |
| `Estado` | ENUM | ABIERTO \| EN_PROCESO \| REMEDIADO \| ACEPTADO \| FALSO_POSITIVO |
| `Cedula` | NVARCHAR(20) | Cédula del empleado involucrado |
| `UsuarioSAP` | NVARCHAR(100) | — |
| `RolAfectado` | NVARCHAR(200) | — |
| `TransaccionesAfectadas` | NVARCHAR(MAX) | — |
| `CasoSESuiteRef` | NVARCHAR(100) | Si hay caso que lo justifique |
| `EvidenciaGenerada` | BIT | Si ya se generaron las evidencias |
| `FechaDeteccion` | DATETIME | — |
| `FechaLimiteRemediacion` | DATE | — |
| `AsignadoA` | NVARCHAR(200) | Responsable de remediar |

**Tabla Evidencias**
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` (PK) | GUID | — |
| `HallazgoId` | GUID | FK a Hallazgos |
| `SimulacionId` | GUID | FK a Simulación (redundante para consultas directas) |
| `TipoEvidencia` | ENUM | CAPTURA_SAP \| REPORTE_ACCESOS \| CASO_SESUITE \| ANALISIS_IA \| PLANILLA_EXCEL \| PDF_AUDITORIA |
| `Titulo` | NVARCHAR(300) | — |
| `Descripcion` | NVARCHAR(1000) | — |
| `ArchivoBlobUrl` | NVARCHAR(500) | URL en Azure Blob Storage |
| `NombreArchivo` | NVARCHAR(300) | — |
| `TamanoBytes` | BIGINT | — |
| `GeneradaPor` | ENUM | MANUAL \| AUTOMATICA_IA \| SISTEMA |
| `FechaGeneracion` | DATETIME | — |
| `CargadaPor` | NVARCHAR(200) | — |

---

## 7. MÓDULOS FUNCIONALES

### 7.1 Dashboard Principal

El dashboard muestra el estado de madurez de auditoría en tiempo real:

**KPIs principales:**
- Semáforo de madurez de accesos SAP (verde/amarillo/rojo)
- % de empleados activos en Nómina vs activos en SAP
- % de empleados activos en Nómina vs activos en Entra ID
- Total de conflictos SoD activos sin justificación
- Hallazgos abiertos por nivel de riesgo
- Simulaciones ejecutadas este trimestre

### 7.2 Simulaciones — Motor de Inteligencia Central

> Ver Sección 23 para especificación completa.

La simulación es el proceso que toma múltiples fuentes de datos, las cruza con las reglas configuradas y genera hallazgos automáticos con evidencias.

**Tipos de simulación disponibles:**

| Tipo | Descripción | Fuentes requeridas |
|---|---|---|
| `ACCESOS_SAP` | Valida que cada usuario SAP activo esté en Nómina y EntraID | SAP Roles + Nómina + EntraID |
| `CRUCE_NOMINA` | Valida que cada empleado activo en Nómina tenga cuenta SAP y EntraID | Nómina + SAP + EntraID |
| `SOD` | Detecta conflictos de Segregación de Funciones | SAP Roles + Matriz SoD |
| `MATRIZ_PUESTOS` | Compara roles reales SAP contra la Matriz de Puestos aprobada | SAP Roles + Matriz Puestos + Casos SE Suite |
| `COMPLETO` | Ejecuta todos los análisis en secuencia | Todas las fuentes |
| `PERSONALIZADO` | El usuario define qué validaciones ejecutar | Según selección |

### 7.3 Hallazgos

Hallazgos generados automáticamente por simulaciones o creados manualmente.

**Flujo de hallazgo:**
```
Simulación detecta discrepancia
        ↓
Hallazgo creado automáticamente (TipoHallazgo + NivelRiesgo asignado por reglas)
        ↓
Notificación al responsable asignado
        ↓
Responsable revisa → puede marcar como Falso Positivo o iniciar remediación
        ↓
Se puede adjuntar Caso SE Suite como justificación → hallazgo se cierra como ACEPTADO
        ↓
Auditor genera evidencia del hallazgo (individual o masiva)
        ↓
Evidencias disponibles para entrega a auditores externos
```

### 7.4 Planes de Acción

Cada hallazgo puede tener un plan de acción con:
- Descripción de la acción correctiva
- Responsable asignado
- Fecha límite
- Estado: PENDIENTE / EN_PROCESO / COMPLETADO / VENCIDO
- Evidencias adjuntas al plan

### 7.5 Evidencias

Las evidencias son la **prueba documental** de que un hallazgo existe o fue corregido.

**Tipos de evidencia:**

| Tipo | Cómo se genera | Contenido |
|---|---|---|
| `REPORTE_ACCESOS` | Automático desde simulación | Excel con todos los accesos del usuario |
| `CAPTURA_SAP` | Manual — el auditor sube la imagen/PDF | Pantalla SAP con el acceso |
| `CASO_SESUITE` | Cargado en módulo Cargas | PDF o imagen del caso aprobado |
| `ANALISIS_IA` | Generado por Agente IA | Análisis detallado del hallazgo en formato Word |
| `PLANILLA_EXCEL` | Generación automática | Excel con todos los hallazgos y detalle |
| `PDF_AUDITORIA` | Generación automática | Informe formal para auditores externos |

### 7.6 Cargas Masivas

**Fuentes de carga disponibles:**

| Tipo | Descripción | Plantilla |
|---|---|---|
| Empleados Nómina | Maestro de empleados Evolution | `plantilla_empleados.xlsx` |
| Usuarios Sistema | Usuarios de cualquier sistema (AD, SAP básico) | `plantilla_usuarios.xlsx` |
| SAP Roles y Transacciones | Export de V_SAP_USR_RECERTIFICAION — UNA FILA POR USUARIO+ROL+TCODE | `plantilla_sap_roles.xlsx` |
| Matriz de Puestos | Misma estructura SAP + FechaRevisionContraloria | `plantilla_matriz_puestos.xlsx` |
| Casos SE Suite | Reporte de casos aprobados | `plantilla_casos_sesuite.xlsx` |
| Imágenes/PDFs Casos | Archivos de evidencia individuales de cada caso | Upload múltiple |

**Estructura plantilla SAP Roles / Matriz de Puestos:**
```
USUARIO | NOMBRE_COMPLETO | SOCIEDAD | DEPARTAMENTO | PUESTO | EMAIL |
ROL | INICIO_VALIDEZ | FIN_VALIDEZ | TRANSACCION | ULTIMO_INGRESO
```
> La columna `USUARIO` contiene el ID técnico SAP. La columna primera del Excel original tiene el `ID` (cédula), que se mapea al campo `Cedula` en la base de datos.

**Estructura plantilla Casos SE Suite:**
```
NUMERO_CASO | TITULO | USUARIO_SAP | CEDULA | ROL_JUSTIFICADO |
TRANSACCIONES_JUSTIFICADAS | FECHA_APROBACION | FECHA_VENCIMIENTO |
ESTADO | APROBADOR
```

### 7.7 Conectores SOA

Conectores de integración directa con sistemas externos:
- **BASE_DATOS**: SQL Server (Azure o on-premise vía Hybrid Connections)
- **REST_API**: Endpoints HTTP/HTTPS
- **SFTP**: Transferencia de archivos
- **WEBHOOK**: Receptores de eventos

Cada conector tiene: probar conectividad, ejecutar y visualizar resultados, historial de ejecuciones.

### 7.8 Agente IA

> Ver Sección 10 para especificación completa.

El Agente IA puede responder preguntas sobre los datos cargados. Ejemplos:

- *"¿Cuántos usuarios SAP están activos pero inactivos en nómina?"*
- *"¿Qué usuarios tienen el rol Z_FI_CUENTAS_PAGAR y no tienen caso SE Suite?"*
- *"¿Hay conflictos SoD sin justificar en el área de finanzas?"*
- *"Muéstrame todos los accesos de la cédula 1-0001-0001"*

### 7.9 Políticas

Repositorio de políticas de control interno. Cada política puede:
- Tener versiones con control de cambios
- Vincularse a controles de simulación
- Generar alertas cuando está próxima a vencer

### 7.10 Bitácora

Registro inmutable de todas las acciones de todos los usuarios. Incluye:
- Login/logout
- Creación/modificación/eliminación de registros
- Ejecución de simulaciones
- Generación de evidencias
- Cargas de archivos

---

## 8. MOTOR DE REGLAS DE AUDITORÍA

### Reglas de Control Cruzado de Accesos

Las reglas se evalúan en secuencia durante la simulación. Cada regla positiva genera un hallazgo automático.

#### Regla R01 — Usuario SAP activo sin registro en Nómina
```
Condición: UsuariosSistema.Sistema='SAP' AND Estado='ACTIVO'
           AND NOT EXISTS (EmpleadosMaestro WHERE Cedula = UsuariosSistema.Cedula)
Resultado: Hallazgo CRITICO — "Usuario SAP sin empleado en nómina"
Tipo: ACCESO_INDEBIDO
Severidad: CRITICO (empleado fantasma con acceso a sistemas)
```

#### Regla R02 — Usuario SAP activo con empleado inactivo en Nómina
```
Condición: UsuariosSistema.Sistema='SAP' AND Estado='ACTIVO'
           AND EmpleadosMaestro.Cedula = UsuariosSistema.Cedula
           AND EmpleadosMaestro.EstadoLaboral != 'ACTIVO'
Resultado: Hallazgo ALTO — "Empleado dado de baja con acceso SAP activo"
Tipo: USUARIO_INACTIVO_CON_ACCESO
Severidad: ALTO (acceso de ex-empleado)
```

#### Regla R03 — Empleado activo en Nómina sin cuenta Entra ID
```
Condición: EmpleadosMaestro.EstadoLaboral='ACTIVO'
           AND (EntraIdObject IS NULL OR EntraIdObject = '')
Resultado: Hallazgo MEDIO — "Empleado activo sin cuenta corporativa Entra ID"
Tipo: DISCREPANCIA_ENTRAID
Severidad: MEDIO
```

#### Regla R04 — Empleado activo en Nómina sin usuario SAP (cuando debería tenerlo según Matriz de Puestos)
```
Condición: EmpleadosMaestro.EstadoLaboral='ACTIVO'
           AND MatrizPuestosRol contiene el Puesto del empleado
           AND NOT EXISTS (UsuariosSistema WHERE Cedula = empleado.Cedula AND Sistema='SAP')
Resultado: Hallazgo MEDIO — "Empleado activo sin acceso SAP según Matriz de Puestos"
Tipo: DISCREPANCIA_NOMINA
Severidad: MEDIO
```

#### Regla R05 — Rol SAP sin caso SE Suite que lo justifique (exceso vs Matriz de Puestos)
```
Condición: AsignacionesRolesUsuarios.Activa = 1
           AND RolSistema no está en MatrizPuestosRol para el Puesto del usuario
           AND NOT EXISTS (CasosSESuite WHERE UsuarioSAP = usuario AND RolJustificado = rol AND Estado='APROBADO' AND (FechaVencimiento IS NULL OR FechaVencimiento >= GETDATE()))
Resultado: Hallazgo ALTO — "Rol SAP adicional sin justificación en SE Suite"
Tipo: ROL_SIN_JUSTIFICACION
Severidad: ALTO
```

#### Regla R06 — Conflicto de Segregación de Funciones (SoD)
```
Condición: El usuario tiene asignados simultáneamente RolA y RolB
           Y (RolA, RolB) existe en MatrizConflictosSoD
           AND NOT EXISTS caso SE Suite que justifique la excepción
Resultado: Hallazgo CRITICO/ALTO según nivel de la matriz SoD
Tipo: SOD_CONFLICTO
Severidad: Según NivelRiesgo en la matriz SoD
```

#### Regla R07 — Transacción SAP no incluida en ningún rol de la Matriz de Puestos
```
Condición: La transacción está asignada al usuario en SAP
           AND la transacción NO está en ningún rol de la Matriz de Puestos para el puesto del usuario
           AND no hay caso SE Suite que la justifique
Resultado: Hallazgo ALTO — "Transacción SAP fuera de perfil de puesto"
Tipo: ACCESO_INDEBIDO
Severidad: ALTO
```

#### Regla R08 — Caso SE Suite vencido con acceso activo
```
Condición: CasoSESuite.Estado='APROBADO' AND FechaVencimiento < GETDATE()
           AND AsignacionRolUsuario sigue Activa
Resultado: Hallazgo MEDIO — "Acceso justificado por caso SE Suite vencido"
Tipo: ROL_SIN_JUSTIFICACION
Severidad: MEDIO
```

---

## 9. MOTOR DE CONTROL CRUZADO DE ACCESOS

### El triángulo de validación

```
                    CÉDULA DE IDENTIDAD
                           │
          ┌────────────────┼────────────────┐
          │                │                │
          ▼                ▼                ▼
    ┌──────────┐    ┌──────────────┐   ┌───────────────┐
    │   SAP    │    │  Evolution   │   │ Microsoft     │
    │          │    │  (Nómina)    │   │ Entra ID      │
    │ Usuario  │    │              │   │               │
    │ Roles    │◄──►│ Empleado     │◄──►│ Cuenta        │
    │ Tcodes   │    │ Estado       │   │ Corporativa   │
    │ Puesto   │    │ Puesto       │   │ Activa/Inact. │
    └──────────┘    └──────────────┘   └───────────────┘
          │                │                │
          └────────────────┴────────────────┘
                    Matriz de Puestos
                (referencia Contraloría)
                           +
                    Casos SE Suite
                 (justificaciones)
```

### Diagrama de flujo del motor de cruce

```
INICIO SIMULACIÓN
      │
      ▼
Cargar fuentes de datos seleccionadas
[SAP Roles] [Nómina] [Matriz Puestos] [Casos SE Suite] [EntraID]
      │
      ▼
FASE 1: Normalización
  ├── Crear índice por cédula: { cedula → empleado, usuario SAP, cuenta AAD }
  ├── Crear índice por puesto: { puesto → roles esperados según Matriz }
  └── Crear índice de justificaciones: { usuario+rol → caso SE Suite vigente }
      │
      ▼
FASE 2: Validaciones de existencia (Reglas R01-R04)
  ├── Por cada usuario SAP activo → buscar en nómina por cédula
  ├── Por cada empleado nómina activo → buscar en SAP y EntraID
  └── Registrar discrepancias
      │
      ▼
FASE 3: Validaciones de roles (Reglas R05, R07, R08)
  ├── Por cada asignación SAP → comparar con Matriz de Puestos
  ├── Si excede matriz → buscar caso SE Suite vigente
  └── Sin caso = hallazgo; con caso = aceptado
      │
      ▼
FASE 4: Validaciones SoD (Regla R06)
  ├── Por cada par de roles del usuario → buscar en Matriz SoD
  └── Conflicto sin excepción = hallazgo crítico
      │
      ▼
FASE 5: Generar hallazgos
  ├── Crear registros en tabla Hallazgos
  ├── Clasificar por TipoHallazgo y NivelRiesgo
  └── Calcular métricas del dashboard
      │
      ▼
FASE 6: Actualizar dashboard de resultados
  ├── Gráfico de dona: Hallazgos por tipo
  ├── Gráfico de barras: Hallazgos por nivel de riesgo
  ├── Gráfico de barras apiladas: Estado de empleados por sistema
  └── Tabla detalle: todos los hallazgos con filtros
      │
      ▼
FIN → Estado simulación = COMPLETADA o CON_HALLAZGOS
```

### Algoritmo de cruce por cédula (pseudocódigo)

```python
# Para cada registro del reporte SAP (V_SAP_USR_RECERTIFICAION):
for sap_user in sap_users:
    cedula = sap_user.ID  # Campo ID del reporte = cédula de identidad
    
    nomina = buscar_nomina(cedula)
    entraid = buscar_entraid(cedula)
    puesto_matriz = buscar_matriz_puestos(sap_user.PUESTO)
    
    # Validación 1: Empleado activo
    if nomina is None:
        crear_hallazgo(CRITICO, "Usuario SAP sin empleado en nómina", cedula)
    elif nomina.estado != "ACTIVO":
        crear_hallazgo(ALTO, "Ex-empleado con acceso SAP activo", cedula)
    
    # Validación 2: Cuenta Entra ID
    if entraid is None and nomina.estado == "ACTIVO":
        crear_hallazgo(MEDIO, "Empleado sin cuenta Entra ID", cedula)
    
    # Validación 3: Roles vs Matriz de Puestos
    for rol_asignado in sap_user.roles:
        if rol_asignado not in puesto_matriz.roles_esperados:
            caso = buscar_caso_sesuite(sap_user.USUARIO, rol_asignado)
            if caso is None or caso.vencido:
                crear_hallazgo(ALTO, "Rol fuera de Matriz de Puestos sin justificación")
    
    # Validación 4: SoD
    for (rol_a, rol_b) in combinaciones(sap_user.roles):
        if conflicto_sod_existe(rol_a, rol_b):
            caso = buscar_caso_sesuite_sod(sap_user.USUARIO, rol_a, rol_b)
            if caso is None:
                crear_hallazgo(CRITICO, f"Conflicto SoD: {rol_a} + {rol_b}")
```

---

## 10. AGENTE IA AUDITOR PREVENTIVO

### Capacidades del agente con datos de accesos

El Agente IA tiene acceso a toda la base de conocimiento de la plataforma más los datos cargados. Puede responder:

**Consultas de accesos:**
- *"¿Cuántos usuarios SAP están activos pero dados de baja en nómina?"*
- *"Lista los usuarios con conflictos SoD sin justificación"*
- *"¿Qué transacciones tiene asignadas el empleado con cédula 1-0001-0001?"*
- *"¿Hay usuarios SAP que no aparecen en la última simulación?"*

**Consultas de riesgo:**
- *"¿Cuáles son los 10 roles más riesgosos asignados en producción?"*
- *"¿Qué áreas tienen más hallazgos de acceso indebido?"*
- *"¿Cuántos casos SE Suite vencerán en los próximos 30 días?"*

**Consultas de cumplimiento:**
- *"¿Estamos listos para la auditoría? Muéstrame el resumen"*
- *"¿Qué hallazgos críticos no tienen plan de acción asignado?"*
- *"Genera el informe ejecutivo de la simulación más reciente"*

### Contexto que el agente usa en sus respuestas

El agente construye su contexto combinando:
1. **Base de conocimiento RAG** (documentos de auditoría indexados)
2. **Métricas en tiempo real** de la DB (número de hallazgos, usuarios, etc.)
3. **Historial de simulaciones** (qué se encontró y cuándo)
4. **Reglas de negocio** configuradas en el motor de reglas

### Guardrails del agente

- Solo responde preguntas relacionadas con auditoría, accesos, riesgos y controles
- No revela contraseñas ni información sensible completa (solo últimos 4 caracteres de identificadores)
- Siempre cita la fuente de la información (simulación N, archivo X, fecha Y)
- Si no tiene los datos para responder: *"No tengo esa información cargada. ¿Deseas ejecutar una simulación?"*

---

## 11. MOTOR DE INTEGRACIONES — SOA MANAGER

### Conectores disponibles

| Tipo | Sistemas | Configuración |
|---|---|---|
| `BASE_DATOS` | SQL Server (Azure SQL o on-premise) | Servidor, base, usuario, password, vista/query |
| `REST_API` | Cualquier API REST/JSON | URL endpoint, tipo de auth, headers |
| `SFTP` | Transferencia de archivos | Host, puerto, credenciales |
| `WEBHOOK` | Receptores de eventos | URL, método, payload template |

### Arquitectura de conectividad para SQL on-premise

Para conectores SQL Server on-premise (como el servidor `CLDPROBPMBD01`):
- **Opción 1 (recomendada):** Azure Hybrid Connections — agente relay en el servidor, sin VPN
- **Opción 2:** VNet Peering + VNet Integration en App Service (requiere misma región Azure)
- **Opción 3:** Puerto 1433 abierto con IP fija en NSG (solo si servidor tiene IP pública)

Para instancias nombradas: siempre usar puerto fijo (`servidor,1433`) — evitar SQL Browser.

---

## 12. UX/UI — DISEÑO FIORI ENTERPRISE

### Principios de diseño

- **Claridad sobre complejidad:** Semáforos de color (verde/amarillo/rojo) para riesgo
- **Densidad de información:** Tablas con filtros, ordenamiento y paginación nativa
- **Acción inmediata:** Cada hallazgo tiene botón de acción directa
- **Mobile-ready:** Breakpoints para tablet (auditores en campo)
- **Dark mode ready:** Variable CSS preparadas

### Paleta de colores por riesgo

| Nivel | Color | Hex | Uso |
|---|---|---|---|
| CRITICO | Rojo intenso | `#dc2626` | SoD, ex-empleados con acceso |
| ALTO | Rojo suave | `#ef4444` | Roles sin justificación |
| MEDIO | Naranja | `#f97316` | Discrepancias Entra ID |
| BAJO | Amarillo | `#eab308` | Observaciones menores |
| OK | Verde | `#22c55e` | Sin hallazgos |
| INFO | Azul | `#3b82f6` | Información general |

---

## 13. TRAZABILIDAD Y BITÁCORA

Todo evento en la plataforma genera un registro inmutable:

```
{ userId, email, accion, entidad, idEntidad, datosPrevios, datosDespues, ip, timestamp }
```

Acciones trazadas: LOGIN, LOGOUT, CREAR, ACTUALIZAR, ELIMINAR, EJECUTAR_SIMULACION, GENERAR_EVIDENCIA, CARGAR_ARCHIVO, EXPORTAR, CONSULTA_AGENTE_IA

---

## 14. GENERACIÓN DE ENTREGABLES

### Tipos de exportación

| Formato | Contenido | Endpoint |
|---|---|---|
| Excel (.xlsx) | Resultados de simulación, hallazgos, accesos detallados | `GET /simulaciones/{id}/exportar/excel` |
| Excel (.xlsx) | Todos los hallazgos con filtros | `GET /hallazgos/exportar/excel` |
| PDF | Informe formal de auditoría | `GET /simulaciones/{id}/exportar/pdf` |
| Word (.docx) | Análisis de hallazgo individual por IA | `GET /hallazgos/{id}/evidencia/word` |
| ZIP | Paquete completo de evidencias para auditores | `GET /simulaciones/{id}/evidencias/zip` |

### Estructura del Excel de resultados de simulación

```
Hoja 1: Resumen Ejecutivo
  - Nombre y objetivo de la simulación
  - Métricas: Total analizado, hallazgos por tipo y riesgo
  - Fuentes de datos utilizadas

Hoja 2: Hallazgos Detallados
  - Código | Tipo | Riesgo | Cédula | Usuario SAP | Rol | Descripción | Estado | Fecha | Responsable

Hoja 3: Accesos SAP con Discrepancias
  - Todos los registros SAP que generaron hallazgos con el detalle completo

Hoja 4: Empleados sin acceso SAP (cuando deberían tenerlo)
  - Empleados activos en Nómina sin usuario SAP según Matriz de Puestos

Hoja 5: Conflictos SoD
  - Pares de roles en conflicto por usuario con nivel de riesgo
```

---

## 15. API INTERNA

### Endpoints principales v2.0

```
POST   /api/simulaciones                           Crear simulación
GET    /api/simulaciones                           Listar simulaciones
GET    /api/simulaciones/{id}                      Detalle de simulación
POST   /api/simulaciones/{id}/ejecutar             Ejecutar simulación (genera hallazgos)
GET    /api/simulaciones/{id}/resultados           Dashboard de resultados (KPIs + gráficos)
GET    /api/simulaciones/{id}/exportar/excel       Exportar resultados a Excel
GET    /api/simulaciones/{id}/exportar/pdf         Exportar informe PDF
POST   /api/simulaciones/{id}/evidencias/generar   Generar evidencias masivas

GET    /api/hallazgos                              Listar hallazgos (con filtros)
GET    /api/hallazgos/{id}                         Detalle hallazgo
PUT    /api/hallazgos/{id}/estado                  Cambiar estado
POST   /api/hallazgos/{id}/evidencia               Generar evidencia individual
POST   /api/hallazgos/{id}/caso-sesuite            Vincular caso SE Suite como justificación
POST   /api/hallazgos/exportar/excel               Exportar hallazgos a Excel

POST   /api/cargas/sap-roles                       Cargar SAP V_SAP_USR_RECERTIFICAION
POST   /api/cargas/matriz-puestos                  Cargar Matriz de Puestos de Contraloría
POST   /api/cargas/casos-sesuite                   Cargar reporte de casos SE Suite
POST   /api/cargas/casos-sesuite/archivo/{id}      Subir imagen/PDF de un caso específico
GET    /api/cargas/plantilla/sap-roles             Descargar plantilla SAP
GET    /api/cargas/plantilla/matriz-puestos        Descargar plantilla Matriz de Puestos
GET    /api/cargas/plantilla/casos-sesuite         Descargar plantilla Casos SE Suite

POST   /api/conectores                             Crear conector
PUT    /api/conectores/{id}                        Actualizar conector
POST   /api/conectores/{id}/probar                 Probar conectividad
POST   /api/conectores/{id}/ejecutar               Ejecutar y obtener datos
POST   /api/conectores/{id}/probar-query           Probar query sin guardar

GET    /api/agente/consultar                       Consulta al Agente IA
POST   /api/evidencias/subir                       Subir archivo de evidencia
GET    /api/evidencias/{id}/descargar              Descargar evidencia
```

---

## 16. ESCENARIOS DE PRUEBA — MATRIZ QA

### Casos de prueba críticos para Motor de Cruce

| ID | Escenario | Datos entrada | Resultado esperado |
|---|---|---|---|
| QA-01 | Usuario SAP sin registro en nómina | UsuarioSAP={cedula no existe en nómina} | Hallazgo CRITICO generado |
| QA-02 | Ex-empleado con SAP activo | UsuarioSAP + EmpleadoBaja | Hallazgo ALTO generado |
| QA-03 | Empleado activo sin cuenta AAD | EmpleadoActivo + EntraIdObject=NULL | Hallazgo MEDIO generado |
| QA-04 | Rol SAP fuera de Matriz con caso vigente | Rol extra + CasoAprobadoVigente | Sin hallazgo (justificado) |
| QA-05 | Rol SAP fuera de Matriz sin caso | Rol extra + sin caso SE Suite | Hallazgo ALTO generado |
| QA-06 | Conflicto SoD sin excepción | RolA + RolB en MatrizSoD | Hallazgo CRITICO |
| QA-07 | Conflicto SoD con caso vigente | RolA + RolB + CasoAprobado | Sin hallazgo |
| QA-08 | Carga SAP → agrupación por usuario+rol | 100 filas mismo usuario+rol, 10 tcodes | 1 asignación con 10 tcodes concatenados |
| QA-09 | Exportar Excel de simulación | Simulación con 50 hallazgos | Excel con 5 hojas y 50 filas en hoja 2 |
| QA-10 | Generación masiva de evidencias | 20 hallazgos en simulación | 20 archivos en Blob + 20 registros en BD |

---

## 17. ROADMAP DE IMPLEMENTACIÓN

### Fase 1 — Completada ✅
- ✅ Infraestructura Azure (App Service, SQL, Blob, OpenAI, Key Vault, Static Web App)
- ✅ Autenticación Entra ID con MSAL Browser (PKCE)
- ✅ Módulos base: Simulaciones, Hallazgos, Evidencias, Agente IA, Conectores, Bitácora
- ✅ Cargas: Empleados, Usuarios Sistema, SAP Roles (V_SAP_USR_RECERTIFICAION)
- ✅ Módulo Base de Conocimiento (RAG con Azure OpenAI)

### Fase 2 — En desarrollo 🔄
- 🔄 Campo `Cedula` (ID) en carga SAP como clave de cruce
- 🔄 Carga **Matriz de Puestos** (+ FechaRevisionContraloria)
- 🔄 Carga **Casos SE Suite** (reporte + archivos individuales)
- 🔄 **Motor de Control Cruzado de Accesos** (reglas R01-R08)
- 🔄 **Dashboard de Resultados de Simulación** con gráficos Recharts
- 🔄 Simulaciones: tabla de fuentes de datos + descripción del objetivo
- 🔄 **Generación masiva de evidencias** desde hallazgos
- 🔄 Vinculación Caso SE Suite ↔ Hallazgo (justificación)
- 🔄 Exportación Excel de resultados de simulación

### Fase 3 — Planificada 📋
- 📋 Conector SQL Server on-premise vía Azure Hybrid Connections
- 📋 Integración directa Microsoft Graph API (lectura estado EntraID en tiempo real)
- 📋 Notificaciones por email (hallazgos críticos, planes de acción vencidos)
- 📋 Dashboard ejecutivo con KPIs de madurez (1-10 por área)
- 📋 Generación automática de informe Word/PPT para Contraloría
- 📋 Recertificación periódica con workflow de aprobación
- 📋 App móvil (React Native o PWA) para auditores en campo

---

## 18. ALINEACIÓN NORMATIVA

| Marco | Controles relevantes | Cobertura en AuditorPRO TI |
|---|---|---|
| COBIT 5 / COBIT 2019 | APO01, DSS05, MEA02 | Control de accesos, segregación, evidencias |
| ISO 27001:2022 | A.9 (Control de Accesos) | Gestión de identidades, SoD, revisión periódica |
| NIST CSF 2.0 | PR.AC (Gestión de Identidad) | Validación cruzada SAP+Nómina+AAD |
| SOX | Segregación de funciones | Motor SoD con trazabilidad |
| Regulaciones Locales CR | CONASSIF, SUGEF (si aplica) | Evidencias, bitácora, planes de acción |

---

## 19. GESTIÓN DE CONFIGURACIÓN Y AMBIENTES

| Ambiente | URL | Propósito |
|---|---|---|
| Producción | https://polite-coast-0d4936110.6.azurestaticapps.net | Uso real ILG Logistics |
| Backend Prod | https://z-aud-app-auditorpro.azurewebsites.net | API en Azure App Service |
| Base de datos | sql-trackdocs-ilg.database.windows.net / Z-AUD-DB-auditorpro | Azure SQL |
| Storage | zaudstauditorpro.blob.core.windows.net | Evidencias y documentos |
| OpenAI | Z-AUD-OAI-auditorpro (eastus) | GPT-4o para agente IA |
| Key Vault | Z-AUD-KV-auditorpro | Secretos de producción |

---

## 20. ARQUITECTURA DE DESPLIEGUE AZURE

```
Internet
    │
    ▼
Azure Static Web Apps (Z-AUD-SWA-auditorpro)
  └── React App (Frontend)
  └── Routing: /api/** → Azure App Service (proxied)
    │
    ▼
Azure App Service (Z-AUD-APP-auditorpro) — Linux .NET 10
  └── AuditorPRO.Api
  └── Managed Identity → Key Vault, SQL, Blob, OpenAI
    │
    ├── Azure SQL Database (Z-AUD-DB-auditorpro)
    │     sql-trackdocs-ilg.database.windows.net
    │
    ├── Azure Blob Storage (zaudstauditorpro)
    │     Container: evidencias, casos-sesuite, documentos
    │
    ├── Azure OpenAI (Z-AUD-OAI-auditorpro, eastus)
    │     Deployment: gpt-4o
    │
    └── Azure Key Vault (Z-AUD-KV-auditorpro)
          Secrets: SqlConnectionString, AzureStorage--ConnectionString, AzureOpenAI--ApiKey
```

---

## 21. CHECKLIST DE VERIFICACIÓN FINAL

### Pre-deploy
- [ ] Variables de entorno configuradas en Key Vault
- [ ] Migración SQL aplicada (columnas nuevas: Cedula, FechaRevisionContraloria)
- [ ] Plantillas Excel actualizadas (sap-roles, matriz-puestos, casos-sesuite)
- [ ] Motor de cruce registrado en DI container

### Post-deploy
- [ ] Cargar datos de prueba: empleados nómina + SAP roles + Matriz Puestos
- [ ] Ejecutar simulación tipo COMPLETO
- [ ] Verificar que se generan hallazgos automáticos
- [ ] Verificar dashboard de resultados con gráficos
- [ ] Verificar exportación a Excel
- [ ] Verificar generación de evidencias

---

## 22. BASE DE CONOCIMIENTO — RAG LOCAL

El sistema indexa automáticamente todos los documentos cargados en Azure Blob Storage y los hace disponibles para el Agente IA mediante búsqueda semántica.

### Documentos que debe contener la base de conocimiento

| Tipo | Descripción | Relevancia para auditoría |
|---|---|---|
| Políticas de acceso | Documento de política de gestión de identidades | Define quién puede tener qué |
| Matriz SoD | Tabla de conflictos de segregación de funciones | Regla de oro de control SAP |
| Marcos normativos | COBIT, ISO 27001, políticas CGAI | Contexto regulatorio |
| Procedimientos | SOP de gestión de usuarios SAP | Proceso correcto |
| Reportes anteriores | Informes de auditorías anteriores | Historial de debilidades |

---

## 23. SIMULACIONES — MOTOR DE INTELIGENCIA CENTRAL

### Diseño de la página de Simulaciones

#### Panel 1: Crear/configurar simulación

```
┌──────────────────────────────────────────────────────────────────┐
│  Nueva Simulación                                                  │
│                                                                    │
│  Nombre: [________________________________]                        │
│                                                                    │
│  Objetivo/Descripción:                                             │
│  [Describe qué busca esta simulación, ej: "Validar que todos los  │
│   usuarios SAP estén activos en Nómina y Entra ID"...]             │
│  [Textarea grande]                                                  │
│                                                                    │
│  Tipo de simulación: [▼ COMPLETO / ACCESOS_SAP / SoD / ...]        │
│  Sociedad: [▼ ILG-CR / ILG-SV / ...]                               │
│                                                                    │
│  ┌─ Fuentes de datos ──────────────────────────────────────────┐   │
│  │  Tipo                │ Archivo          │ Registros │ Estado │   │
│  │  SAP Roles           │ sap_extract.xlsx │ 1,234     │ ✅ OK  │   │
│  │  Empleados Nómina    │ nomina_abr.xlsx  │   456     │ ✅ OK  │   │
│  │  Matriz de Puestos   │ matriz_v3.xlsx   │    89     │ ✅ OK  │   │
│  │  Casos SE Suite      │ casos_q1.xlsx    │    34     │ ✅ OK  │   │
│  │  Entra ID            │ [via Graph API]  │   456     │ ✅ OK  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                    │
│  [▶ Ejecutar Simulación]  [Guardar borrador]                       │
└──────────────────────────────────────────────────────────────────┘
```

#### Panel 2: Resultados — Dashboard de simulación

```
┌──────────────────────────────────────────────────────────────────┐
│  Resultados: Validación Accesos SAP — Abril 2026                   │
│  Estado: ● CON HALLAZGOS  │  Ejecutada: 13/04/2026 10:35 AM       │
│                                                                    │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐     │
│  │  1,234     │ │    47      │ │    12      │ │    35      │     │
│  │ Registros  │ │ Hallazgos  │ │ Críticos   │ │ Abiertos   │     │
│  │ analizados │ │ generados  │ │            │ │            │     │
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘     │
│                                                                    │
│  ┌─ Por tipo ──────────────┐  ┌─ Por riesgo ───────────────────┐  │
│  │ [Gráfico dona Recharts] │  │ [Gráfico barras horizontales]  │  │
│  │  Acceso indebido: 18    │  │  ██████████ CRITICO: 12        │  │
│  │  Ex-empleado acceso: 8  │  │  ████████ ALTO: 20             │  │
│  │  Sin justificación: 15  │  │  ████ MEDIO: 11               │  │
│  │  Conflicto SoD: 6      │  │  ██ BAJO: 4                    │  │
│  └─────────────────────────┘  └────────────────────────────────┘  │
│                                                                    │
│  ┌─ Cobertura de sistemas ────────────────────────────────────┐   │
│  │ [Gráfico barras apiladas]                                   │   │
│  │  En SAP + Nómina + Entra ID: 389 (85%)                     │   │
│  │  Solo en SAP: 12 (3%) ← hallazgos                          │   │
│  │  Solo en Nómina: 8 (2%) ← hallazgos                        │   │
│  │  En Nómina + Entra ID, no SAP: 47 (10%)                    │   │
│  └────────────────────────────────────────────────────────────┘   │
│                                                                    │
│  [📥 Exportar Excel]  [📄 Generar Informe PDF]                     │
│  [🛡 Generar todas las evidencias]  [↗ Ver hallazgos detalle]      │
└──────────────────────────────────────────────────────────────────┘
```

#### Panel 3: Tabla detalle de hallazgos de la simulación

```
┌────────────────────────────────────────────────────────────────────┐
│  Hallazgos de la simulación (47)          [Filtrar▼] [Exportar⬇]   │
│                                                                     │
│  Código   │ Tipo            │ Riesgo  │ Cédula      │ Usuario SAP  │
│  HAL-001  │ Ex-emp. acceso  │ ● ALTO  │ 1-0234-0123 │ MPEREZ       │
│  HAL-002  │ Acceso indebido │ ● CRIT  │ 2-0567-0890 │ AGARCIA      │
│  HAL-003  │ Sin justif. SE  │ ● ALTO  │ 1-0345-0234 │ JLOPEZ       │
│  HAL-004  │ SoD: FI+MM      │ ● CRIT  │ 3-0123-0456 │ RHERRERA     │
│  ...                                                                │
│                                                                     │
│  [◀ 1 2 3 ... 5 ▶]                                                  │
└────────────────────────────────────────────────────────────────────┘
```

---

## 24. DASHBOARD DE RESULTADOS DE SIMULACIÓN

### Gráficos implementados con Recharts

#### Gráfico 1: Hallazgos por tipo (DonutChart)
```typescript
// Datos del gráfico
const dataTipo = [
  { name: 'Acceso Indebido', value: 18, color: '#dc2626' },
  { name: 'Ex-empleado con Acceso', value: 8, color: '#ef4444' },
  { name: 'Rol sin Justificación', value: 15, color: '#f97316' },
  { name: 'Conflicto SoD', value: 6, color: '#b91c1c' },
];
// Componente: <PieChart> con innerRadius para efecto dona
```

#### Gráfico 2: Hallazgos por nivel de riesgo (BarChart horizontal)
```typescript
const dataNivel = [
  { nivel: 'CRITICO', total: 12, color: '#dc2626' },
  { nivel: 'ALTO',    total: 20, color: '#ef4444' },
  { nivel: 'MEDIO',   total: 11, color: '#f97316' },
  { nivel: 'BAJO',    total: 4,  color: '#eab308' },
];
// Componente: <BarChart layout="vertical">
```

#### Gráfico 3: Cobertura de sistemas (StackedBarChart)
```typescript
const dataCobertura = [
  { sistema: 'SAP + Nómina + AAD', empleados: 389, porcentaje: 85 },
  { sistema: 'Solo SAP', empleados: 12, porcentaje: 3 },   // ← hallazgo
  { sistema: 'Solo Nómina', empleados: 8, porcentaje: 2 },  // ← hallazgo
  { sistema: 'Sin SAP (debería tener)', empleados: 47, porcentaje: 10 },
];
```

#### Gráfico 4: Tendencia de hallazgos (LineChart — entre simulaciones)
```typescript
// Compara métricas entre la simulación actual y las anteriores
// Muestra si la organización mejora o empeora en el tiempo
```

### Exportación a Excel — Estructura

**Hoja 1: Resumen Ejecutivo**
- Nombre de la simulación y objetivo
- Fecha de ejecución y ejecutada por
- Métricas principales (KPIs)
- Fuentes de datos utilizadas con su descripción

**Hoja 2: Hallazgos Detallados**
- Código, Tipo, Riesgo, Cédula, UsuarioSAP, Nombre, Departamento, Puesto
- Rol Afectado, Transacciones, Caso SE Suite ref, Estado, Fecha, Responsable

**Hoja 3: Accesos SAP — Discrepancias**
- Todos los registros SAP que presentaron discrepancias
- Columna "Discrepancia" con descripción

**Hoja 4: Conflictos SoD**
- Pares de roles conflictivos por usuario

**Hoja 5: Empleados sin SAP (cuando aplica)**
- Empleados activos en nómina que deberían tener acceso según Matriz

---

## 25. CONTROL DE VERSIONES DEL BLUEPRINT

| Versión | Fecha | Autor | Cambios |
|---|---|---|---|
| v1.0 | Feb 2026 | ILG TI + Claude Sonnet | Versión inicial — arquitectura base, módulos, Clean Architecture |
| v1.1 | Mar 2026 | ILG TI + Claude Sonnet | Módulo Base de Conocimiento RAG, OpenAI GPT-4o |
| v2.0 | Abr 2026 | ILG TI + Claude Sonnet | Motor Control Cruzado de Accesos (SAP+Nómina+EntraID por cédula), Matriz de Puestos, Casos SE Suite, Dashboard Simulaciones con gráficos Recharts, Generación masiva evidencias, Vinculación SE Suite ↔ Hallazgos |

---

*Documento clasificado — Uso interno ILG Logistics. Prohibida su distribución externa.*
*© 2026 ILG Logistics — Área de TI y Auditoría Interna*
