-- CatalogDb: довідникові дані. Повторний запуск не створює дублікатів.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE CatalogDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
-- Категорії
MERGE dbo.Categories AS t
USING (VALUES
  (N'Електроніка', N'electronics'),
  (N'Комп''ютери', N'computers'),
  (N'Периферія',   N'peripherals'),
  (N'Аудіо',       N'audio')
) AS s(Name, Slug) ON t.Slug = s.Slug
WHEN NOT MATCHED THEN INSERT (Name, Slug) VALUES (s.Name, s.Slug);

-- Товари: Id 1..5 збігаються з OrdersDb.Products та reviews.productId у MongoDB
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT dbo.Products (Id, Sku, Name, Price, CreatedBy) VALUES
      (1, N'NB-X15',  N'Ноутбук X15',          32999.00, N'seed'),
      (2, N'MS-M200', N'Бездротова миша M200',   799.00, N'seed'),
      (3, N'KB-K550', N'Клавіатура K550',       1499.00, N'seed'),
      (4, N'MN-27Q',  N'Монітор 27" QHD',       8999.00, N'seed'),
      (5, N'HP-H9',   N'Навушники H9',          2499.00, N'seed');
    SET IDENTITY_INSERT dbo.Products OFF;
END

-- 1:1 деталі
INSERT dbo.ProductDetails (ProductId, Description, Manufacturer, WeightGrams, WarrantyMonths)
SELECT v.ProductId, v.Description, v.Manufacturer, v.WeightGrams, v.WarrantyMonths
FROM (VALUES
  (1, N'15.6" IPS, 16 ГБ RAM, 512 ГБ SSD', N'TechCorp', 1800, 24),
  (2, N'Бездротова миша 2.4 ГГц',          N'TechCorp',   90, 12),
  (3, N'Механічна клавіатура',             N'KeyWorks',  850, 12),
  (4, N'27" QHD IPS, 75 Гц',               N'ViewMax',  5200, 36),
  (5, N'Накладні навушники з Bluetooth',   N'SoundLab',  280, 12)
) AS v(ProductId, Description, Manufacturer, WeightGrams, WarrantyMonths)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductDetails d WHERE d.ProductId = v.ProductId);

-- 1:N картинки
INSERT dbo.ProductImages (ProductId, Url, SortOrder, IsMain)
SELECT v.ProductId, v.Url, v.SortOrder, v.IsMain
FROM (VALUES
  (1, N'/img/nb-x15-1.jpg', 0, 1),
  (1, N'/img/nb-x15-2.jpg', 1, 0),
  (2, N'/img/ms-m200-1.jpg', 0, 1),
  (3, N'/img/kb-k550-1.jpg', 0, 1),
  (4, N'/img/mn-27q-1.jpg',  0, 1),
  (5, N'/img/hp-h9-1.jpg',   0, 1)
) AS v(ProductId, Url, SortOrder, IsMain)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductImages i WHERE i.ProductId = v.ProductId AND i.Url = v.Url);

-- M:N (з власними полями)
INSERT dbo.ProductCategories (ProductId, CategoryId, IsPrimary)
SELECT v.ProductId, c.Id, v.IsPrimary
FROM (VALUES
  (1, N'computers',   1), (1, N'electronics', 0),
  (2, N'peripherals', 1), (2, N'computers',   0),
  (3, N'peripherals', 1),
  (4, N'peripherals', 1), (4, N'electronics', 0),
  (5, N'audio',       1), (5, N'electronics', 0)
) AS v(ProductId, Slug, IsPrimary)
JOIN dbo.Categories c ON c.Slug = v.Slug
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductCategories pc WHERE pc.ProductId = v.ProductId AND pc.CategoryId = c.Id);
GO

-- Перевірки
SELECT p.Id, p.Name, p.Price, c.Name AS Category, pc.IsPrimary, pc.AddedAt
FROM dbo.Products p
JOIN dbo.ProductCategories pc ON pc.ProductId = p.Id
JOIN dbo.Categories c ON c.Id = pc.CategoryId
ORDER BY p.Id;
SELECT * FROM sys.foreign_keys;   -- усі FK — лише всередині CatalogDb
