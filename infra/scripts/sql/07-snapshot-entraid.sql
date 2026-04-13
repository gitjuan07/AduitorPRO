-- ============================================================
-- Migración 07 — Snapshots de Entra ID (Azure Active Directory)
-- Crea las tablas SnapshotsEntraID y RegistrosEntraID
-- Cada carga genera una cabecera (snapshot) + filas de detalle.
-- EmployeeId es la cédula de identidad — campo clave de cruce
-- ============================================================

-- ── Tabla cabecera ──────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SnapshotsEntraID')
BEGIN
    CREATE TABLE [dbo].[SnapshotsEntraID] (
        [Id]               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [Nombre]           NVARCHAR(200)    NOT NULL,
        [FechaInstantanea] DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        [TotalRegistros]   INT              NOT NULL DEFAULT 0,
        [CreadoPor]        NVARCHAR(200)    NULL,
        CONSTRAINT [PK_SnapshotsEntraID] PRIMARY KEY ([Id])
    );
    PRINT 'Tabla SnapshotsEntraID creada.';
END
ELSE
    PRINT 'Tabla SnapshotsEntraID ya existe — omitida.';
GO

-- ── Índice por fecha para ordenar historial ──────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SnapshotsEntraID_FechaInstantanea'
      AND object_id = OBJECT_ID('dbo.SnapshotsEntraID')
)
BEGIN
    CREATE INDEX [IX_SnapshotsEntraID_FechaInstantanea]
        ON [dbo].[SnapshotsEntraID] ([FechaInstantanea] DESC);
    PRINT 'Índice IX_SnapshotsEntraID_FechaInstantanea creado.';
END
GO

-- ── Tabla detalle ────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RegistrosEntraID')
BEGIN
    CREATE TABLE [dbo].[RegistrosEntraID] (
        [Id]                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [SnapshotId]          UNIQUEIDENTIFIER NOT NULL,
        [EmployeeId]          NVARCHAR(50)     NULL,   -- Cédula de identidad — clave de cruce
        [ObjectId]            NVARCHAR(100)    NULL,   -- Object ID inmutable en Entra ID
        [DisplayName]         NVARCHAR(200)    NULL,
        [UserPrincipalName]   NVARCHAR(300)    NULL,
        [Email]               NVARCHAR(300)    NULL,
        [Department]          NVARCHAR(200)    NULL,
        [JobTitle]            NVARCHAR(200)    NULL,
        [AccountEnabled]      BIT              NOT NULL DEFAULT 1,
        [Manager]             NVARCHAR(200)    NULL,
        [OfficeLocation]      NVARCHAR(200)    NULL,
        [CreatedDateTime]     DATETIME2        NULL,
        [LastSignInDateTime]  DATETIME2        NULL,
        CONSTRAINT [PK_RegistrosEntraID] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RegistrosEntraID_SnapshotsEntraID]
            FOREIGN KEY ([SnapshotId])
            REFERENCES [dbo].[SnapshotsEntraID] ([Id])
            ON DELETE CASCADE
    );
    PRINT 'Tabla RegistrosEntraID creada.';
END
ELSE
    PRINT 'Tabla RegistrosEntraID ya existe — omitida.';
GO

-- ── Índices de cruce ────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RegistrosEntraID_SnapshotId_EmployeeId'
      AND object_id = OBJECT_ID('dbo.RegistrosEntraID')
)
BEGIN
    CREATE INDEX [IX_RegistrosEntraID_SnapshotId_EmployeeId]
        ON [dbo].[RegistrosEntraID] ([SnapshotId], [EmployeeId]);
    PRINT 'Índice IX_RegistrosEntraID_SnapshotId_EmployeeId creado.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RegistrosEntraID_EmployeeId'
      AND object_id = OBJECT_ID('dbo.RegistrosEntraID')
)
BEGIN
    CREATE INDEX [IX_RegistrosEntraID_EmployeeId]
        ON [dbo].[RegistrosEntraID] ([EmployeeId])
        WHERE [EmployeeId] IS NOT NULL;
    PRINT 'Índice IX_RegistrosEntraID_EmployeeId creado.';
END
GO

PRINT '✓ Migración 07 completada.';
