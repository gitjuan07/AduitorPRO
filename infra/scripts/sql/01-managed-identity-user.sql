-- ================================================================
-- AuditorPRO TI — Configurar Managed Identity del App Service
-- Ejecutar en la base de datos: Z-AUD-DB-auditorpro
-- Servidor: sql-trackdocs-ilg.database.windows.net
-- ================================================================
-- PASO 1: Conectar al servidor SQL como Azure AD Admin
-- (juan.serranopw@ilglogistics.com ya está configurado como AAD Admin)
--
-- PASO 2: Ejecutar en la base de datos Z-AUD-DB-auditorpro:

USE [Z-AUD-DB-auditorpro];

-- Crear usuario para la Managed Identity del App Service
CREATE USER [Z-AUD-APP-auditorpro] FROM EXTERNAL PROVIDER;

-- Dar permisos necesarios para EF Core y operaciones de auditoría
ALTER ROLE db_datareader ADD MEMBER [Z-AUD-APP-auditorpro];
ALTER ROLE db_datawriter ADD MEMBER [Z-AUD-APP-auditorpro];
ALTER ROLE db_ddladmin ADD MEMBER [Z-AUD-APP-auditorpro];
