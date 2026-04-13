-- Agrega columna TransaccionesAutorizadas a RolesSistema
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'RolesSistema' AND COLUMN_NAME = 'TransaccionesAutorizadas'
)
BEGIN
    ALTER TABLE [RolesSistema]
    ADD [TransaccionesAutorizadas] nvarchar(max) NULL;

    PRINT 'Columna TransaccionesAutorizadas agregada a RolesSistema';
END
ELSE
BEGIN
    PRINT 'Columna TransaccionesAutorizadas ya existe en RolesSistema';
END
