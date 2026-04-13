-- ============================================================================
-- AuditorPRO TI v2.0 — Motor de Control Cruzado de Accesos
-- Script: 06-v2-cross-control-accesos.sql
-- Descripción: Agrega la cédula de identidad como clave maestra de cruce
--              tridimensional SAP ↔ Nómina ↔ Entra ID. Crea tablas para
--              Matriz de Puestos, Casos SE Suite y Fuentes de Datos de Simulación.
-- Ejecutar en: sql-trackdocs-ilg.database.windows.net / Z-AUD-DB-auditorpro
-- ============================================================================

PRINT '=== AuditorPRO TI v2.0 — Migracion Control Cruzado de Accesos ===';

-- ============================================================================
-- 1. Columna Cedula en EmpleadosMaestro (clave de cruce con SAP y Entra ID)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='Empleados' AND COLUMN_NAME='Cedula')
BEGIN
    ALTER TABLE [Empleados] ADD [Cedula] nvarchar(20) NULL;
    CREATE INDEX [IX_Empleados_Cedula] ON [Empleados]([Cedula]) WHERE [Cedula] IS NOT NULL;
    PRINT '+ Empleados.Cedula agregada';
END

-- ============================================================================
-- 2. Columnas en UsuariosSistema (datos del reporte SAP V_SAP_USR_RECERTIFICAION)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='UsuariosSistema' AND COLUMN_NAME='Cedula')
BEGIN
    ALTER TABLE [UsuariosSistema] ADD [Cedula] nvarchar(20) NULL;
    CREATE INDEX [IX_UsuariosSistema_Cedula] ON [UsuariosSistema]([Sistema],[Cedula]) WHERE [Cedula] IS NOT NULL;
    PRINT '+ UsuariosSistema.Cedula agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='UsuariosSistema' AND COLUMN_NAME='NombreCompleto')
BEGIN
    ALTER TABLE [UsuariosSistema] ADD [NombreCompleto] nvarchar(200) NULL;
    PRINT '+ UsuariosSistema.NombreCompleto agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='UsuariosSistema' AND COLUMN_NAME='Sociedad')
BEGIN
    ALTER TABLE [UsuariosSistema] ADD [Sociedad] nvarchar(50) NULL;
    PRINT '+ UsuariosSistema.Sociedad agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='UsuariosSistema' AND COLUMN_NAME='Departamento')
BEGIN
    ALTER TABLE [UsuariosSistema] ADD [Departamento] nvarchar(100) NULL;
    PRINT '+ UsuariosSistema.Departamento agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='UsuariosSistema' AND COLUMN_NAME='Puesto')
BEGIN
    ALTER TABLE [UsuariosSistema] ADD [Puesto] nvarchar(100) NULL;
    PRINT '+ UsuariosSistema.Puesto agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='UsuariosSistema' AND COLUMN_NAME='Email')
BEGIN
    ALTER TABLE [UsuariosSistema] ADD [Email] nvarchar(200) NULL;
    PRINT '+ UsuariosSistema.Email agregada';
END

-- ============================================================================
-- 3. CasoSESuiteRef en AsignacionesRolesUsuarios
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='AsignacionesRolesUsuarios' AND COLUMN_NAME='CasoSESuiteRef')
BEGIN
    ALTER TABLE [AsignacionesRolesUsuarios] ADD [CasoSESuiteRef] nvarchar(100) NULL;
    PRINT '+ AsignacionesRolesUsuarios.CasoSESuiteRef agregada';
END

-- ============================================================================
-- 4. Columna TransaccionesAutorizadas en RolesSistema (si no existe aun)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='RolesSistema' AND COLUMN_NAME='TransaccionesAutorizadas')
BEGIN
    ALTER TABLE [RolesSistema] ADD [TransaccionesAutorizadas] nvarchar(max) NULL;
    PRINT '+ RolesSistema.TransaccionesAutorizadas agregada';
END

-- ============================================================================
-- 5. Tabla MatrizPuestosSAP (Matriz de Puestos aprobada por Contraloria)
-- Estructura identica a V_SAP_USR_RECERTIFICAION + FechaRevisionContraloria
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='MatrizPuestosSAP')
BEGIN
    CREATE TABLE [MatrizPuestosSAP] (
        [Id]                        uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [Cedula]                    nvarchar(20) NULL,
        [UsuarioSAP]                nvarchar(100) NOT NULL DEFAULT '',
        [NombreCompleto]            nvarchar(200) NULL,
        [Sociedad]                  nvarchar(50) NULL,
        [Departamento]              nvarchar(100) NULL,
        [Puesto]                    nvarchar(100) NOT NULL DEFAULT '',
        [Email]                     nvarchar(200) NULL,
        [Rol]                       nvarchar(200) NOT NULL DEFAULT '',
        [InicioValidez]             date NULL,
        [FinValidez]                date NULL,
        [Transaccion]               nvarchar(100) NULL,
        [UltimoIngreso]             datetime2 NULL,
        [FechaRevisionContraloria]  date NULL,         -- Default: 31/07/2025
        [LoteCargaId]               uniqueidentifier NULL,
        -- BaseEntity fields
        [CreatedAt]                 datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]                 datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]                 nvarchar(200) NULL,
        [IsDeleted]                 bit NOT NULL DEFAULT 0,
        CONSTRAINT [PK_MatrizPuestosSAP] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_MatrizPuestosSAP_PuestoRol] ON [MatrizPuestosSAP]([Puesto],[Rol]);
    CREATE INDEX [IX_MatrizPuestosSAP_Cedula] ON [MatrizPuestosSAP]([Cedula]) WHERE [Cedula] IS NOT NULL;
    PRINT '+ Tabla MatrizPuestosSAP creada';
END

-- ============================================================================
-- 6. Tabla CasosSESuite (justificaciones de acceso aprobadas en SE Suite)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='CasosSESuite')
BEGIN
    CREATE TABLE [CasosSESuite] (
        [Id]                        uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [NumeroCaso]                nvarchar(50) NOT NULL,
        [Titulo]                    nvarchar(500) NULL,
        [UsuarioSAP]                nvarchar(100) NULL,
        [Cedula]                    nvarchar(20) NULL,
        [RolJustificado]            nvarchar(200) NULL,
        [TransaccionesJustificadas] nvarchar(max) NULL,
        [FechaAprobacion]           date NULL,
        [FechaVencimiento]          date NULL,
        [EstadoCaso]                nvarchar(50) NOT NULL DEFAULT 'APROBADO',
        [Aprobador]                 nvarchar(200) NULL,
        [ArchivoAdjuntoUrl]         nvarchar(500) NULL,
        [LoteCargaId]               uniqueidentifier NULL,
        -- BaseEntity fields
        [CreatedAt]                 datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]                 datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]                 nvarchar(200) NULL,
        [IsDeleted]                 bit NOT NULL DEFAULT 0,
        CONSTRAINT [PK_CasosSESuite] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_CasosSESuite_NumeroCaso] UNIQUE ([NumeroCaso])
    );
    CREATE INDEX [IX_CasosSESuite_UsuarioRol] ON [CasosSESuite]([UsuarioSAP],[RolJustificado]);
    CREATE INDEX [IX_CasosSESuite_Cedula] ON [CasosSESuite]([Cedula]) WHERE [Cedula] IS NOT NULL;
    PRINT '+ Tabla CasosSESuite creada';
END

-- ============================================================================
-- 7. Tabla FuentesDatosSimulacion (fuentes de datos usadas en cada simulacion)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='FuentesDatosSimulacion')
BEGIN
    CREATE TABLE [FuentesDatosSimulacion] (
        [Id]              uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [SimulacionId]    uniqueidentifier NOT NULL,
        [TipoFuente]      nvarchar(50) NOT NULL,
        [NombreArchivo]   nvarchar(300) NOT NULL DEFAULT '',
        [Descripcion]     nvarchar(1000) NOT NULL DEFAULT '',
        [FechaCarga]      datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [TotalRegistros]  int NOT NULL DEFAULT 0,
        [LoteCargaId]     uniqueidentifier NULL,
        CONSTRAINT [PK_FuentesDatosSimulacion] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_FuentesDatosSimulacion_SimulacionId] ON [FuentesDatosSimulacion]([SimulacionId]);
    PRINT '+ Tabla FuentesDatosSimulacion creada';
END

-- ============================================================================
-- 8. Columnas adicionales en SimulacionesAuditoria para v2.0
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='SimulacionesAuditoria' AND COLUMN_NAME='Objetivo')
BEGIN
    ALTER TABLE [SimulacionesAuditoria] ADD [Objetivo] nvarchar(2000) NULL;
    PRINT '+ SimulacionesAuditoria.Objetivo agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='SimulacionesAuditoria' AND COLUMN_NAME='TipoSimulacion')
BEGIN
    ALTER TABLE [SimulacionesAuditoria] ADD [TipoSimulacion] nvarchar(50) NULL DEFAULT 'COMPLETO';
    PRINT '+ SimulacionesAuditoria.TipoSimulacion agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='SimulacionesAuditoria' AND COLUMN_NAME='ResumenResultados')
BEGIN
    ALTER TABLE [SimulacionesAuditoria] ADD [ResumenResultados] nvarchar(max) NULL;
    PRINT '+ SimulacionesAuditoria.ResumenResultados agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='SimulacionesAuditoria' AND COLUMN_NAME='TotalCriticos')
BEGIN
    ALTER TABLE [SimulacionesAuditoria] ADD [TotalCriticos] int NOT NULL DEFAULT 0;
    PRINT '+ SimulacionesAuditoria.TotalCriticos agregada';
END

-- ============================================================================
-- 9. Columnas adicionales en Hallazgos para v2.0
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='Hallazgos' AND COLUMN_NAME='Cedula')
BEGIN
    ALTER TABLE [Hallazgos] ADD [Cedula] nvarchar(20) NULL;
    PRINT '+ Hallazgos.Cedula agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='Hallazgos' AND COLUMN_NAME='UsuarioSAP')
BEGIN
    ALTER TABLE [Hallazgos] ADD [UsuarioSAP] nvarchar(100) NULL;
    PRINT '+ Hallazgos.UsuarioSAP agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='Hallazgos' AND COLUMN_NAME='RolAfectado')
BEGIN
    ALTER TABLE [Hallazgos] ADD [RolAfectado] nvarchar(200) NULL;
    PRINT '+ Hallazgos.RolAfectado agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='Hallazgos' AND COLUMN_NAME='TransaccionesAfectadas')
BEGIN
    ALTER TABLE [Hallazgos] ADD [TransaccionesAfectadas] nvarchar(max) NULL;
    PRINT '+ Hallazgos.TransaccionesAfectadas agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='Hallazgos' AND COLUMN_NAME='CasoSESuiteRef')
BEGIN
    ALTER TABLE [Hallazgos] ADD [CasoSESuiteRef] nvarchar(100) NULL;
    PRINT '+ Hallazgos.CasoSESuiteRef agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='Hallazgos' AND COLUMN_NAME='EvidenciaGenerada')
BEGIN
    ALTER TABLE [Hallazgos] ADD [EvidenciaGenerada] bit NOT NULL DEFAULT 0;
    PRINT '+ Hallazgos.EvidenciaGenerada agregada';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='Hallazgos' AND COLUMN_NAME='TipoHallazgo')
BEGIN
    ALTER TABLE [Hallazgos] ADD [TipoHallazgo] nvarchar(50) NULL;
    PRINT '+ Hallazgos.TipoHallazgo agregada';
END

PRINT '';
PRINT '=== Migracion v2.0 completada exitosamente ===';
