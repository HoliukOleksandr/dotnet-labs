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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Sku] nvarchar(50) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [AvailableStock] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Products_AvailableStock] CHECK ([AvailableStock] >= 0),
        CONSTRAINT [CK_Products_Price] CHECK ([Price] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductCategories] (
        [ProductId] bigint NOT NULL,
        [CategoryId] int NOT NULL,
        [AddedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsPrimary] bit NOT NULL,
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([ProductId], [CategoryId]),
        CONSTRAINT [FK_ProductCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductCategories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductDetails] (
        [ProductId] bigint NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Manufacturer] nvarchar(150) NULL,
        [WeightGrams] int NULL,
        [WarrantyMonths] int NOT NULL,
        CONSTRAINT [PK_ProductDetails] PRIMARY KEY ([ProductId]),
        CONSTRAINT [CK_ProductDetails_Warranty] CHECK ([WarrantyMonths] >= 0),
        CONSTRAINT [CK_ProductDetails_Weight] CHECK ([WeightGrams] IS NULL OR [WeightGrams] > 0),
        CONSTRAINT [FK_ProductDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductImages] (
        [Id] bigint NOT NULL IDENTITY,
        [ProductId] bigint NOT NULL,
        [Url] nvarchar(500) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsMain] bit NOT NULL,
        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'Slug') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([Id], [Name], [Slug])
    VALUES (1, N''Електроніка'', N''electronics''),
    (2, N''Комп''''ютери'', N''computers''),
    (3, N''Периферія'', N''peripherals''),
    (4, N''Аудіо'', N''audio'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'Slug') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductCategories_CategoryId_AddedAt] ON [ProductCategories] ([CategoryId], [AddedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductCategories_ProductId] ON [ProductCategories] ([ProductId]) WHERE [IsPrimary] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]) WHERE [IsMain] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductImages_ProductId_SortOrder] ON [ProductImages] ([ProductId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Price] ON [Products] ([Price]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112225307_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112225307_InitialCreate', N'9.0.23');
END;

COMMIT;
GO

