-- ============================================================
-- Проєкт №1: OrdersDb (контекст «Замовлення», SQL Server)
-- Запуск: sqlcmd -S . -E -I -i schema.sql   (-I = QUOTED_IDENTIFIER ON)
-- Скрипт ідемпотентний: можна запускати повторно.
-- ============================================================
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
IF DB_ID(N'OrdersDb') IS NULL CREATE DATABASE OrdersDb;
GO
USE OrdersDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO

-- Customers (в eShop — Buyer). Власник даних — цей контекст.
IF OBJECT_ID(N'dbo.Customers') IS NULL
CREATE TABLE dbo.Customers (
    Id        BIGINT IDENTITY(1,1) CONSTRAINT PK_Customers PRIMARY KEY,
    Email     NVARCHAR(256) NOT NULL CONSTRAINT UQ_Customers_Email UNIQUE,
    FullName  NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NULL,
    UpdatedAt DATETIME2     NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT           NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT 0,
    RowVersion ROWVERSION
);
GO

-- Orders: 1:N  Customers -> Orders
IF OBJECT_ID(N'dbo.Orders') IS NULL
CREATE TABLE dbo.Orders (
    Id         BIGINT IDENTITY(1,1) CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId BIGINT       NOT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers(Id), -- без CASCADE: історія клієнта не видаляється
    Status     NVARCHAR(30) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT 'Submitted'
        CONSTRAINT CK_Orders_Status CHECK (Status IN ('Submitted','AwaitingValidation','StockConfirmed','Paid','Shipped','Cancelled')),
    OrderDate  DATETIME2    NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT SYSUTCDATETIME(),
    CreatedAt  DATETIME2    NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy  NVARCHAR(100) NULL,
    UpdatedAt  DATETIME2    NULL,
    UpdatedBy  NVARCHAR(100) NULL,
    IsDeleted  BIT          NOT NULL CONSTRAINT DF_Orders_IsDeleted DEFAULT 0,
    RowVersion ROWVERSION   -- оптимістична конкурентність: двоє користувачів не затруть зміни один одного
);
GO

-- OrderDetails: 1:1  Orders <-> OrderDetails (PK = FK)
IF OBJECT_ID(N'dbo.OrderDetails') IS NULL
CREATE TABLE dbo.OrderDetails (
    OrderId         BIGINT NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY
                    CONSTRAINT FK_OrderDetails_Orders REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    ShippingAddress NVARCHAR(400)  NOT NULL,
    Comment         NVARCHAR(1000) NULL
);
GO

-- Products: ЛОКАЛЬНА КОПІЯ довідника Каталогу (власник — CatalogDb). Id той самий, FK між БД немає.
IF OBJECT_ID(N'dbo.Products') IS NULL
CREATE TABLE dbo.Products (
    Id       BIGINT NOT NULL CONSTRAINT PK_Products PRIMARY KEY,   -- не IDENTITY: Id генерує Каталог
    Name     NVARCHAR(200)  NOT NULL,
    Price    DECIMAL(18,2)  NOT NULL CONSTRAINT CK_Products_Price CHECK (Price > 0),
    Stock    INT            NOT NULL CONSTRAINT DF_Products_Stock DEFAULT 0 CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
    SyncedAt DATETIME2      NOT NULL CONSTRAINT DF_Products_SyncedAt DEFAULT SYSUTCDATETIME()
);
GO

-- OrderItems: M:N  Orders <-> Products (проміжна таблиця з власними полями)
IF OBJECT_ID(N'dbo.OrderItems') IS NULL
CREATE TABLE dbo.OrderItems (
    OrderId     BIGINT NOT NULL CONSTRAINT FK_OrderItems_Orders   REFERENCES dbo.Orders(Id)   ON DELETE CASCADE,
    ProductId   BIGINT NOT NULL CONSTRAINT FK_OrderItems_Products REFERENCES dbo.Products(Id),
    ProductName NVARCHAR(200) NOT NULL,                 -- ЗНІМОК на момент замовлення, не оновлюється
    UnitPrice   DECIMAL(18,2) NOT NULL CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice > 0), -- ЗНІМОК
    Discount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderItems_Discount DEFAULT 0 CONSTRAINT CK_OrderItems_Discount CHECK (Discount >= 0),
    Units       INT NOT NULL CONSTRAINT CK_OrderItems_Units CHECK (Units > 0),
    CONSTRAINT PK_OrderItems PRIMARY KEY (OrderId, ProductId)
);
GO

-- OrderStatusHistory: 1:N Orders -> історія статусів
IF OBJECT_ID(N'dbo.OrderStatusHistory') IS NULL
CREATE TABLE dbo.OrderStatusHistory (
    Id        BIGINT IDENTITY(1,1) CONSTRAINT PK_OrderStatusHistory PRIMARY KEY,
    OrderId   BIGINT NOT NULL CONSTRAINT FK_OSH_Orders REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    OldStatus NVARCHAR(30) NULL,
    NewStatus NVARCHAR(30) NOT NULL,
    ChangedAt DATETIME2 NOT NULL CONSTRAINT DF_OSH_ChangedAt DEFAULT SYSUTCDATETIME(),
    ChangedBy NVARCHAR(100) NULL
);
GO

-- PaymentRecords: 1:N Orders -> оплати
IF OBJECT_ID(N'dbo.PaymentRecords') IS NULL
CREATE TABLE dbo.PaymentRecords (
    Id      BIGINT IDENTITY(1,1) CONSTRAINT PK_PaymentRecords PRIMARY KEY,
    OrderId BIGINT NOT NULL CONSTRAINT FK_PaymentRecords_Orders REFERENCES dbo.Orders(Id),
    Amount  DECIMAL(18,2) NOT NULL CONSTRAINT CK_PaymentRecords_Amount CHECK (Amount > 0),
    Method  NVARCHAR(30)  NOT NULL CONSTRAINT DF_PaymentRecords_Method DEFAULT 'Card',
    PaidAt  DATETIME2     NOT NULL CONSTRAINT DF_PaymentRecords_PaidAt DEFAULT SYSUTCDATETIME()
);
GO

-- ===================== ІНДЕКСИ (кожен під конкретний запит) =====================
-- Q1: «замовлення клієнта з певним статусом» (usp_GetCustomerOrders)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Orders_CustomerId_Status' AND object_id=OBJECT_ID('dbo.Orders'))
    CREATE INDEX IX_Orders_CustomerId_Status ON dbo.Orders (CustomerId, Status) INCLUDE (OrderDate);
-- Q2: «активні (не видалені) замовлення клієнта» — фільтрований індекс
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Orders_Active' AND object_id=OBJECT_ID('dbo.Orders'))
    CREATE INDEX IX_Orders_Active ON dbo.Orders (CustomerId) WHERE IsDeleted = 0;
-- Q3: «у яких замовленнях є товар X» (зворотний пошук по M:N; PK покриває лише пошук за OrderId)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_OrderItems_ProductId' AND object_id=OBJECT_ID('dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_ProductId ON dbo.OrderItems (ProductId);
-- Q4: «історія статусів замовлення за часом»
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_OSH_OrderId_ChangedAt' AND object_id=OBJECT_ID('dbo.OrderStatusHistory'))
    CREATE INDEX IX_OSH_OrderId_ChangedAt ON dbo.OrderStatusHistory (OrderId, ChangedAt);
-- Q5: «оплати замовлення»
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PaymentRecords_OrderId' AND object_id=OBJECT_ID('dbo.PaymentRecords'))
    CREATE INDEX IX_PaymentRecords_OrderId ON dbo.PaymentRecords (OrderId);
GO

-- Денормалізоване представлення для читання
CREATE OR ALTER VIEW dbo.vw_OrderSummary AS
SELECT o.Id AS OrderId, o.Status, o.OrderDate, c.Id AS CustomerId, c.FullName, c.Email,
       d.ShippingAddress,
       SUM(i.UnitPrice * i.Units - i.Discount) AS TotalAmount,
       SUM(i.Units) AS TotalUnits
FROM dbo.Orders o
JOIN dbo.Customers c    ON c.Id = o.CustomerId
JOIN dbo.OrderDetails d ON d.OrderId = o.Id
LEFT JOIN dbo.OrderItems i ON i.OrderId = o.Id
WHERE o.IsDeleted = 0
GROUP BY o.Id, o.Status, o.OrderDate, c.Id, c.FullName, c.Email, d.ShippingAddress;
GO
