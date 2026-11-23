-- ============================================================
-- Проєкт №2: CatalogDb (контекст «Каталог товарів», SQL Server)
-- Це проєкт схеми; у Лаб. 03 її буде реалізовано в EF Core (Code First + Fluent API + міграції).
-- Запуск: sqlcmd -S . -E -I -i schema.sql
-- ============================================================
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
IF DB_ID(N'CatalogDb') IS NULL CREATE DATABASE CatalogDb;
GO
USE CatalogDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO

-- Categories (довідник)
IF OBJECT_ID(N'dbo.Categories') IS NULL
CREATE TABLE dbo.Categories (
    Id        INT IDENTITY(1,1) CONSTRAINT PK_Categories PRIMARY KEY,
    Name      NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
    Slug      NVARCHAR(120) NOT NULL CONSTRAINT UQ_Categories_Slug UNIQUE,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO

-- Products — корінь агрегату. Власник Id/Name/Price для всієї системи.
IF OBJECT_ID(N'dbo.Products') IS NULL
CREATE TABLE dbo.Products (
    Id          BIGINT IDENTITY(1,1) CONSTRAINT PK_Products PRIMARY KEY,
    Sku         NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Products_Sku UNIQUE,
    Name        NVARCHAR(200) NOT NULL,
    Price       DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_Price CHECK (Price > 0),
    IsActive    BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NULL,
    UpdatedAt   DATETIME2 NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT 0,
    RowVersion  ROWVERSION                       -- оптимістична конкурентність (у EF Core: [Timestamp])
);
GO

-- ProductDetails: 1:1 Products <-> ProductDetails (PK = FK)
IF OBJECT_ID(N'dbo.ProductDetails') IS NULL
CREATE TABLE dbo.ProductDetails (
    ProductId    BIGINT NOT NULL CONSTRAINT PK_ProductDetails PRIMARY KEY
                 CONSTRAINT FK_ProductDetails_Products REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    Description  NVARCHAR(MAX) NULL,
    Manufacturer NVARCHAR(150) NULL,
    WeightGrams  INT NULL CONSTRAINT CK_ProductDetails_Weight CHECK (WeightGrams IS NULL OR WeightGrams > 0),
    WarrantyMonths INT NOT NULL CONSTRAINT DF_ProductDetails_Warranty DEFAULT 12 CONSTRAINT CK_ProductDetails_Warranty CHECK (WarrantyMonths >= 0)
);
GO

-- ProductImages: 1:N Products -> ProductImages
IF OBJECT_ID(N'dbo.ProductImages') IS NULL
CREATE TABLE dbo.ProductImages (
    Id        BIGINT IDENTITY(1,1) CONSTRAINT PK_ProductImages PRIMARY KEY,
    ProductId BIGINT NOT NULL CONSTRAINT FK_ProductImages_Products REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    Url       NVARCHAR(500) NOT NULL,
    SortOrder INT NOT NULL CONSTRAINT DF_ProductImages_Sort DEFAULT 0,
    IsMain    BIT NOT NULL CONSTRAINT DF_ProductImages_IsMain DEFAULT 0
);
GO

-- ProductCategories: M:N Products <-> Categories, проміжна сутність із власними полями
IF OBJECT_ID(N'dbo.ProductCategories') IS NULL
CREATE TABLE dbo.ProductCategories (
    ProductId  BIGINT NOT NULL CONSTRAINT FK_PC_Products   REFERENCES dbo.Products(Id)   ON DELETE CASCADE,
    CategoryId INT    NOT NULL CONSTRAINT FK_PC_Categories REFERENCES dbo.Categories(Id) ON DELETE CASCADE,
    AddedAt    DATETIME2 NOT NULL CONSTRAINT DF_PC_AddedAt DEFAULT SYSUTCDATETIME(),  -- власне поле зв'язку
    IsPrimary  BIT NOT NULL CONSTRAINT DF_PC_IsPrimary DEFAULT 0,                     -- власне поле зв'язку
    CONSTRAINT PK_ProductCategories PRIMARY KEY (ProductId, CategoryId)
);
GO

-- ===================== ІНДЕКСИ =====================
-- Q1: пошук товару за назвою (префіксний пошук LIKE 'abc%') серед активних, без видалених
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Products_Name_Active' AND object_id=OBJECT_ID('dbo.Products'))
    CREATE INDEX IX_Products_Name_Active ON dbo.Products (Name) INCLUDE (Price) WHERE IsDeleted = 0 AND IsActive = 1;
-- Q2: «товари категорії X» (зворотний пошук по M:N)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PC_CategoryId' AND object_id=OBJECT_ID('dbo.ProductCategories'))
    CREATE INDEX IX_PC_CategoryId ON dbo.ProductCategories (CategoryId, ProductId);
-- Q3: «картинки товару у правильному порядку»
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ProductImages_Product_Sort' AND object_id=OBJECT_ID('dbo.ProductImages'))
    CREATE INDEX IX_ProductImages_Product_Sort ON dbo.ProductImages (ProductId, SortOrder);
-- Q4: у товару лише одна головна картинка (унікальний фільтрований індекс)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_ProductImages_OneMain' AND object_id=OBJECT_ID('dbo.ProductImages'))
    CREATE UNIQUE INDEX UX_ProductImages_OneMain ON dbo.ProductImages (ProductId) WHERE IsMain = 1;
-- Q5: щонайбільше одна основна категорія на товар
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_PC_OnePrimary' AND object_id=OBJECT_ID('dbo.ProductCategories'))
    CREATE UNIQUE INDEX UX_PC_OnePrimary ON dbo.ProductCategories (ProductId) WHERE IsPrimary = 1;
GO
