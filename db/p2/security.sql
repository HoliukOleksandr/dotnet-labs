-- CatalogDb: права «найменших привілеїв» для застосунку на EF Core.
-- Застосунок читає й пише лише у своїй БД. Міграції (зміна схеми) виконуйте окремим адмін-логіном.
-- ВАЖЛИВО: змініть пароль перед реальним використанням.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'catalog_app')
    CREATE LOGIN catalog_app WITH PASSWORD = N'Catalog_App_Passw0rd!', CHECK_POLICY = OFF, DEFAULT_DATABASE = CatalogDb;
GO
USE CatalogDb;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'catalog_app')
    CREATE USER catalog_app FOR LOGIN catalog_app;
GO
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO catalog_app;   -- лише дані, без DDL
GO
SELECT dp.name AS UserName, p.permission_name, p.state_desc
FROM sys.database_permissions p
JOIN sys.database_principals dp ON dp.principal_id = p.grantee_principal_id
WHERE dp.name = N'catalog_app';
GO
