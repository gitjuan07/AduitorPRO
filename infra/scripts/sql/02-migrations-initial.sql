IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Bitacora] (
    [Id] uniqueidentifier NOT NULL,
    [UsuarioId] nvarchar(450) NOT NULL,
    [UsuarioEmail] nvarchar(max) NULL,
    [Accion] int NOT NULL,
    [Recurso] nvarchar(max) NOT NULL,
    [RecursoId] nvarchar(max) NULL,
    [Descripcion] nvarchar(max) NULL,
    [DatosAntes] nvarchar(max) NULL,
    [DatosDespues] nvarchar(max) NULL,
    [IpOrigen] nvarchar(max) NULL,
    [UserAgent] nvarchar(max) NULL,
    [Exitoso] bit NOT NULL,
    [ErrorDetalle] nvarchar(max) NULL,
    [OcurridoAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Bitacora] PRIMARY KEY ([Id])
);

CREATE TABLE [Conectores] (
    [Id] uniqueidentifier NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NULL,
    [TipoConector] int NOT NULL,
    [Sistema] nvarchar(max) NOT NULL,
    [Estado] int NOT NULL,
    [ConfiguracionJson] nvarchar(max) NOT NULL,
    [UrlEndpoint] nvarchar(max) NULL,
    [AuthType] nvarchar(max) NULL,
    [SecretKeyVaultRef] nvarchar(max) NULL,
    [UltimaEjecucion] datetime2 NULL,
    [UltimaEjecucionExito] bit NOT NULL,
    [UltimoError] nvarchar(max) NULL,
    [TotalEjecuciones] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Conectores] PRIMARY KEY ([Id])
);

CREATE TABLE [Dominios] (
    [Id] int NOT NULL IDENTITY,
    [Codigo] nvarchar(max) NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_Dominios] PRIMARY KEY ([Id])
);

CREATE TABLE [Politicas] (
    [Id] uniqueidentifier NOT NULL,
    [Titulo] nvarchar(max) NOT NULL,
    [Codigo] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NULL,
    [Estado] int NOT NULL,
    [NormaReferencia] nvarchar(max) NULL,
    [Responsable] nvarchar(max) NULL,
    [FechaVigencia] date NULL,
    [FechaRevision] date NULL,
    [Version] int NOT NULL,
    [DocumentoUrl] nvarchar(max) NULL,
    [Contenido] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Politicas] PRIMARY KEY ([Id])
);

CREATE TABLE [RolesSistema] (
    [Id] uniqueidentifier NOT NULL,
    [Sistema] nvarchar(max) NOT NULL,
    [NombreRol] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NULL,
    [NivelRiesgo] nvarchar(max) NULL,
    [EsCritico] bit NOT NULL,
    [Activo] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RolesSistema] PRIMARY KEY ([Id])
);

CREATE TABLE [Simulaciones] (
    [Id] uniqueidentifier NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NULL,
    [Tipo] int NOT NULL,
    [Periodicidad] int NULL,
    [Estado] int NOT NULL,
    [SociedadIds] nvarchar(max) NULL,
    [PeriodoInicio] date NOT NULL,
    [PeriodoFin] date NOT NULL,
    [DominioIds] nvarchar(max) NULL,
    [PuntosControlIds] nvarchar(max) NULL,
    [ScoreMadurez] decimal(4,2) NULL,
    [PorcentajeCumplimiento] decimal(5,2) NULL,
    [TotalControles] int NULL,
    [ControlesVerde] int NULL,
    [ControlesAmarillo] int NULL,
    [ControlesRojo] int NULL,
    [IniciadaPor] nvarchar(max) NOT NULL,
    [IniciadaAt] datetime2 NOT NULL,
    [CompletadaAt] datetime2 NULL,
    [DuracionSegundos] int NULL,
    [ErrorDetalle] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Simulaciones] PRIMARY KEY ([Id])
);

CREATE TABLE [Sociedades] (
    [Id] int NOT NULL IDENTITY,
    [Codigo] nvarchar(10) NOT NULL,
    [Nombre] nvarchar(200) NOT NULL,
    [Pais] nvarchar(max) NULL,
    [Activa] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Sociedades] PRIMARY KEY ([Id])
);

CREATE TABLE [LogsConector] (
    [Id] uniqueidentifier NOT NULL,
    [ConectorId] uniqueidentifier NOT NULL,
    [Exitoso] bit NOT NULL,
    [RegistrosProcesados] int NULL,
    [MensajeError] nvarchar(max) NULL,
    [DuracionMs] int NOT NULL,
    [EjecutadoAt] datetime2 NOT NULL,
    [EjecutadoPor] nvarchar(max) NULL,
    CONSTRAINT [PK_LogsConector] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LogsConector_Conectores_ConectorId] FOREIGN KEY ([ConectorId]) REFERENCES [Conectores] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PuntosControl] (
    [Id] int NOT NULL IDENTITY,
    [DominioId] int NOT NULL,
    [Codigo] nvarchar(max) NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NULL,
    [TipoEvaluacion] int NOT NULL,
    [CriticidadBase] int NOT NULL,
    [NormaReferencia] nvarchar(max) NULL,
    [QuerySql] nvarchar(max) NULL,
    [CondicionVerde] nvarchar(max) NULL,
    [CondicionAmarillo] nvarchar(max) NULL,
    [CondicionRojo] nvarchar(max) NULL,
    [EvidenciaRequerida] nvarchar(max) NULL,
    [Activo] bit NOT NULL,
    [VersionRegla] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PuntosControl] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PuntosControl_Dominios_DominioId] FOREIGN KEY ([DominioId]) REFERENCES [Dominios] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ConflictosSoD] (
    [Id] uniqueidentifier NOT NULL,
    [Sistema] nvarchar(max) NULL,
    [RolAId] uniqueidentifier NULL,
    [RolBId] uniqueidentifier NULL,
    [Descripcion] nvarchar(max) NULL,
    [Riesgo] nvarchar(max) NULL,
    [MitigacionDoc] nvarchar(max) NULL,
    [Activo] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ConflictosSoD] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ConflictosSoD_RolesSistema_RolAId] FOREIGN KEY ([RolAId]) REFERENCES [RolesSistema] ([Id]),
    CONSTRAINT [FK_ConflictosSoD_RolesSistema_RolBId] FOREIGN KEY ([RolBId]) REFERENCES [RolesSistema] ([Id])
);

CREATE TABLE [Departamentos] (
    [Id] int NOT NULL IDENTITY,
    [SociedadId] int NOT NULL,
    [Codigo] nvarchar(max) NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [Activo] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Departamentos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Departamentos_Sociedades_SociedadId] FOREIGN KEY ([SociedadId]) REFERENCES [Sociedades] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Puestos] (
    [Id] int NOT NULL IDENTITY,
    [SociedadId] int NOT NULL,
    [Codigo] nvarchar(max) NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [NivelRiesgo] nvarchar(max) NULL,
    [Activo] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Puestos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Puestos_Sociedades_SociedadId] FOREIGN KEY ([SociedadId]) REFERENCES [Sociedades] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ResultadosControl] (
    [Id] uniqueidentifier NOT NULL,
    [SimulacionId] uniqueidentifier NOT NULL,
    [PuntoControlId] int NOT NULL,
    [SociedadId] int NULL,
    [Semaforo] int NOT NULL,
    [Criticidad] int NOT NULL,
    [ResultadoDetalle] nvarchar(max) NULL,
    [DatosEvaluados] nvarchar(max) NULL,
    [EvidenciaEncontrada] nvarchar(max) NULL,
    [EvidenciaFaltante] nvarchar(max) NULL,
    [AnalisisIa] nvarchar(max) NULL,
    [Recomendacion] nvarchar(max) NULL,
    [ResponsableSugerido] nvarchar(max) NULL,
    [FechaCompromisoSug] date NULL,
    [EvaluadoAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ResultadosControl] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ResultadosControl_PuntosControl_PuntoControlId] FOREIGN KEY ([PuntoControlId]) REFERENCES [PuntosControl] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ResultadosControl_Simulaciones_SimulacionId] FOREIGN KEY ([SimulacionId]) REFERENCES [Simulaciones] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ResultadosControl_Sociedades_SociedadId] FOREIGN KEY ([SociedadId]) REFERENCES [Sociedades] ([Id])
);

CREATE TABLE [Empleados] (
    [Id] uniqueidentifier NOT NULL,
    [NumeroEmpleado] nvarchar(30) NOT NULL,
    [NombreCompleto] nvarchar(max) NOT NULL,
    [CorreoCorporativo] nvarchar(max) NULL,
    [EntraIdObject] nvarchar(max) NULL,
    [SociedadId] int NULL,
    [DepartamentoId] int NULL,
    [PuestoId] int NULL,
    [JefeEmpleadoId] uniqueidentifier NULL,
    [EstadoLaboral] int NOT NULL,
    [FechaIngreso] date NULL,
    [FechaBaja] date NULL,
    [FuenteOrigen] nvarchar(max) NULL,
    [LoteCargaId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Empleados] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Empleados_Departamentos_DepartamentoId] FOREIGN KEY ([DepartamentoId]) REFERENCES [Departamentos] ([Id]),
    CONSTRAINT [FK_Empleados_Empleados_JefeEmpleadoId] FOREIGN KEY ([JefeEmpleadoId]) REFERENCES [Empleados] ([Id]),
    CONSTRAINT [FK_Empleados_Puestos_PuestoId] FOREIGN KEY ([PuestoId]) REFERENCES [Puestos] ([Id]),
    CONSTRAINT [FK_Empleados_Sociedades_SociedadId] FOREIGN KEY ([SociedadId]) REFERENCES [Sociedades] ([Id])
);

CREATE TABLE [MatrizPuestoRol] (
    [Id] uniqueidentifier NOT NULL,
    [PuestoId] int NOT NULL,
    [RolId] uniqueidentifier NOT NULL,
    [Tipo] nvarchar(max) NULL,
    [Justificacion] nvarchar(max) NULL,
    [VigenteDesdE] date NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MatrizPuestoRol] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MatrizPuestoRol_Puestos_PuestoId] FOREIGN KEY ([PuestoId]) REFERENCES [Puestos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MatrizPuestoRol_RolesSistema_RolId] FOREIGN KEY ([RolId]) REFERENCES [RolesSistema] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Hallazgos] (
    [Id] uniqueidentifier NOT NULL,
    [SimulacionId] uniqueidentifier NULL,
    [ResultadoControlId] uniqueidentifier NULL,
    [SociedadId] int NULL,
    [Titulo] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NOT NULL,
    [Criticidad] int NOT NULL,
    [Estado] int NOT NULL,
    [NormaAfectada] nvarchar(max) NULL,
    [RiesgoAsociado] nvarchar(max) NULL,
    [ResponsableEmail] nvarchar(max) NULL,
    [FechaCompromiso] date NULL,
    [FechaCierre] date NULL,
    [PlanAccion] nvarchar(max) NULL,
    [AnalisisIa] nvarchar(max) NULL,
    [EvidenciaCierreIds] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Hallazgos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Hallazgos_ResultadosControl_ResultadoControlId] FOREIGN KEY ([ResultadoControlId]) REFERENCES [ResultadosControl] ([Id]),
    CONSTRAINT [FK_Hallazgos_Simulaciones_SimulacionId] FOREIGN KEY ([SimulacionId]) REFERENCES [Simulaciones] ([Id]),
    CONSTRAINT [FK_Hallazgos_Sociedades_SociedadId] FOREIGN KEY ([SociedadId]) REFERENCES [Sociedades] ([Id])
);

CREATE TABLE [UsuariosSistema] (
    [Id] uniqueidentifier NOT NULL,
    [Sistema] nvarchar(max) NOT NULL,
    [NombreUsuario] nvarchar(max) NOT NULL,
    [EmpleadoId] uniqueidentifier NULL,
    [Estado] int NOT NULL,
    [TipoUsuario] nvarchar(max) NULL,
    [FechaUltimoAcceso] datetime2 NULL,
    [FuenteOrigen] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_UsuariosSistema] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UsuariosSistema_Empleados_EmpleadoId] FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados] ([Id])
);

CREATE TABLE [Evidencias] (
    [Id] uniqueidentifier NOT NULL,
    [HallazgoId] uniqueidentifier NULL,
    [SimulacionId] uniqueidentifier NULL,
    [TipoEvidencia] int NOT NULL,
    [NombreArchivo] nvarchar(max) NOT NULL,
    [DescripcionArchivo] nvarchar(max) NULL,
    [BlobUrl] nvarchar(max) NOT NULL,
    [BlobContainer] nvarchar(max) NULL,
    [TamanoBytes] bigint NOT NULL,
    [ContentType] nvarchar(max) NULL,
    [SubidoPor] nvarchar(max) NOT NULL,
    [SubidoAt] datetime2 NOT NULL,
    [Verificada] bit NOT NULL,
    [HashSha256] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Evidencias] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Evidencias_Hallazgos_HallazgoId] FOREIGN KEY ([HallazgoId]) REFERENCES [Hallazgos] ([Id])
);

CREATE TABLE [AsignacionesRol] (
    [Id] uniqueidentifier NOT NULL,
    [UsuarioId] uniqueidentifier NOT NULL,
    [RolId] uniqueidentifier NOT NULL,
    [FechaAsignacion] date NULL,
    [FechaVencimiento] date NULL,
    [AsignadoPor] nvarchar(max) NULL,
    [ExpedienteRef] nvarchar(max) NULL,
    [Activa] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AsignacionesRol] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AsignacionesRol_RolesSistema_RolId] FOREIGN KEY ([RolId]) REFERENCES [RolesSistema] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AsignacionesRol_UsuariosSistema_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [UsuariosSistema] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AsignacionesRol_RolId] ON [AsignacionesRol] ([RolId]);

CREATE INDEX [IX_AsignacionesRol_UsuarioId] ON [AsignacionesRol] ([UsuarioId]);

CREATE INDEX [IX_Bitacora_OcurridoAt] ON [Bitacora] ([OcurridoAt]);

CREATE INDEX [IX_Bitacora_UsuarioId_OcurridoAt] ON [Bitacora] ([UsuarioId], [OcurridoAt]);

CREATE INDEX [IX_ConflictosSoD_RolAId] ON [ConflictosSoD] ([RolAId]);

CREATE INDEX [IX_ConflictosSoD_RolBId] ON [ConflictosSoD] ([RolBId]);

CREATE INDEX [IX_Departamentos_SociedadId] ON [Departamentos] ([SociedadId]);

CREATE INDEX [IX_Empleados_DepartamentoId] ON [Empleados] ([DepartamentoId]);

CREATE INDEX [IX_Empleados_EstadoLaboral_SociedadId] ON [Empleados] ([EstadoLaboral], [SociedadId]);

CREATE INDEX [IX_Empleados_JefeEmpleadoId] ON [Empleados] ([JefeEmpleadoId]);

CREATE UNIQUE INDEX [IX_Empleados_NumeroEmpleado] ON [Empleados] ([NumeroEmpleado]);

CREATE INDEX [IX_Empleados_PuestoId] ON [Empleados] ([PuestoId]);

CREATE INDEX [IX_Empleados_SociedadId] ON [Empleados] ([SociedadId]);

CREATE INDEX [IX_Evidencias_HallazgoId] ON [Evidencias] ([HallazgoId]);

CREATE INDEX [IX_Hallazgos_ResultadoControlId] ON [Hallazgos] ([ResultadoControlId]);

CREATE INDEX [IX_Hallazgos_SimulacionId] ON [Hallazgos] ([SimulacionId]);

CREATE INDEX [IX_Hallazgos_SociedadId] ON [Hallazgos] ([SociedadId]);

CREATE INDEX [IX_LogsConector_ConectorId] ON [LogsConector] ([ConectorId]);

CREATE INDEX [IX_MatrizPuestoRol_PuestoId] ON [MatrizPuestoRol] ([PuestoId]);

CREATE INDEX [IX_MatrizPuestoRol_RolId] ON [MatrizPuestoRol] ([RolId]);

CREATE INDEX [IX_Puestos_SociedadId] ON [Puestos] ([SociedadId]);

CREATE INDEX [IX_PuntosControl_DominioId] ON [PuntosControl] ([DominioId]);

CREATE INDEX [IX_ResultadosControl_PuntoControlId] ON [ResultadosControl] ([PuntoControlId]);

CREATE INDEX [IX_ResultadosControl_SimulacionId_Semaforo] ON [ResultadosControl] ([SimulacionId], [Semaforo]);

CREATE INDEX [IX_ResultadosControl_SociedadId] ON [ResultadosControl] ([SociedadId]);

CREATE INDEX [IX_Simulaciones_Estado_IniciadaAt] ON [Simulaciones] ([Estado], [IniciadaAt]);

CREATE UNIQUE INDEX [IX_Sociedades_Codigo] ON [Sociedades] ([Codigo]);

CREATE INDEX [IX_UsuariosSistema_EmpleadoId] ON [UsuariosSistema] ([EmpleadoId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260408184203_InitialCreate', N'10.0.5');

COMMIT;
GO

