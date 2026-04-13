-- Seed inicial de Sociedad requerido para Cargas Masivas
-- Ejecutar desde: Azure Portal > sql-trackdocs-ilg > Z-AUD-DB-auditorpro > Query Editor
-- Autenticarse con: juan.serranopw@ilglogistics.com

IF NOT EXISTS (SELECT 1 FROM [Sociedades] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [Sociedades] ON;
    INSERT INTO [Sociedades] ([Id], [Codigo], [Nombre], [Activa], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES (1, 'ILG-CR', 'ILG Logistics S.A.', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
    SET IDENTITY_INSERT [Sociedades] OFF;
    PRINT 'Sociedad ILG-CR insertada correctamente.';
END
ELSE
BEGIN
    PRINT 'Sociedad Id=1 ya existe, no se realizaron cambios.';
END
