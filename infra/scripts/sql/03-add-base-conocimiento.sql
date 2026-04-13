-- ================================================================
-- AuditorPRO TI — Migración: Tabla BaseConocimiento (RAG Local)
-- Ejecutar en: Z-AUD-DB-auditorpro
-- Servidor: sql-trackdocs-ilg.database.windows.net
-- ================================================================

BEGIN TRANSACTION;

CREATE TABLE [BaseConocimiento] (
    [Id]                  uniqueidentifier  NOT NULL DEFAULT NEWSEQUENTIALID(),
    [NombreArchivo]       nvarchar(max)     NOT NULL,
    [RutaOriginal]        nvarchar(max)     NOT NULL,
    [TipoArchivo]         nvarchar(20)      NOT NULL,
    [TamanoBytes]         bigint            NOT NULL,
    [TextoCompleto]       nvarchar(max)     NOT NULL,
    [TotalPalabras]       int               NOT NULL DEFAULT 0,
    [TotalPaginas]        int               NOT NULL DEFAULT 0,
    [DominioDetectado]    nvarchar(max)     NULL,
    [ControlesDetectados] nvarchar(max)     NULL,
    [Tags]                nvarchar(max)     NULL,
    [Estado]              nvarchar(20)      NOT NULL DEFAULT 'PROCESADO',
    [ErrorDetalle]        nvarchar(max)     NULL,
    [FuenteIngesta]       nvarchar(20)      NOT NULL DEFAULT 'DIRECTORIO',
    [IngresadoPor]        nvarchar(max)     NULL,
    [CreadoAt]            datetime2         NOT NULL DEFAULT SYSUTCDATETIME(),
    [IsDeleted]           bit               NOT NULL DEFAULT 0,
    CONSTRAINT [PK_BaseConocimiento] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_BaseConocimiento_Estado]       ON [BaseConocimiento] ([Estado]);
CREATE INDEX [IX_BaseConocimiento_TipoArchivo]  ON [BaseConocimiento] ([TipoArchivo]);
CREATE INDEX [IX_BaseConocimiento_CreadoAt]     ON [BaseConocimiento] ([CreadoAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260412_AddBaseConocimiento', N'10.0.5');

COMMIT;
GO
