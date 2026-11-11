-- OrdersDb: права «найменших привілеїв».
-- Застосунок (Лаб. 02) підключається окремим логіном, який бачить лише OrdersDb
-- і працює з даними через збережувані процедури. Пряме INSERT/UPDATE/DELETE у таблиці заборонено.
-- ВАЖЛИВО: змініть пароль перед реальним використанням.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'orders_app')
    CREATE LOGIN orders_app WITH PASSWORD = N'Orders_App_Passw0rd!', CHECK_POLICY = OFF, DEFAULT_DATABASE = OrdersDb;
GO
USE OrdersDb;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'orders_app')
    CREATE USER orders_app FOR LOGIN orders_app;
GO
GRANT SELECT  ON SCHEMA::dbo TO orders_app;               -- читання таблиць і view
GRANT EXECUTE ON SCHEMA::dbo TO orders_app;               -- виклик процедур (запис іде через них)
GRANT EXECUTE ON TYPE::dbo.OrderItemList TO orders_app;   -- передача позицій замовлення (TVP)
GO
-- Перевірка: що дозволено користувачу
SELECT dp.name AS UserName, p.permission_name, p.state_desc, s.name AS OnSchema
FROM sys.database_permissions p
JOIN sys.database_principals dp ON dp.principal_id = p.grantee_principal_id
LEFT JOIN sys.schemas s ON s.schema_id = p.major_id AND p.class = 3
WHERE dp.name = N'orders_app';
GO
