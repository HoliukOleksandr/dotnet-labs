-- OrdersDb: тестові дані. Повторний запуск не створює дублікатів.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE OrdersDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
-- локальна копія довідника Каталогу (ті ж Id, що в CatalogDb)
MERGE dbo.Products AS t
USING (VALUES
  (1, N'Ноутбук X15',            32999.00, 10),
  (2, N'Бездротова миша M200',     799.00, 50),
  (3, N'Клавіатура K550',         1499.00, 30),
  (4, N'Монітор 27" QHD',         8999.00, 15),
  (5, N'Навушники H9',            2499.00, 25)
) AS s(Id, Name, Price, Stock) ON t.Id = s.Id
WHEN NOT MATCHED THEN INSERT (Id, Name, Price, Stock) VALUES (s.Id, s.Name, s.Price, s.Stock);

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'olena@example.com')
    INSERT dbo.Customers (Email, FullName, CreatedBy) VALUES (N'olena@example.com', N'Олена Коваль', N'seed');
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'andriy@example.com')
    INSERT dbo.Customers (Email, FullName, CreatedBy) VALUES (N'andriy@example.com', N'Андрій Мельник', N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    DECLARE @c1 BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'olena@example.com');
    DECLARE @c2 BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'andriy@example.com');
    DECLARE @o1 BIGINT, @o2 BIGINT, @o3 BIGINT;
    DECLARE @i1 dbo.OrderItemList, @i2 dbo.OrderItemList, @i3 dbo.OrderItemList;
    INSERT @i1 VALUES (1,1),(2,2);
    INSERT @i2 VALUES (5,1);
    INSERT @i3 VALUES (3,1),(4,1);

    EXEC dbo.usp_CreateOrder @c1, N'Чернівці, вул. Головна 1', N'Подзвонити перед доставкою', @i1, N'seed', @o1 OUTPUT;
    EXEC dbo.usp_CreateOrder @c1, N'Чернівці, вул. Головна 1', NULL, @i2, N'seed', @o2 OUTPUT;
    EXEC dbo.usp_CreateOrder @c2, N'Київ, просп. Перемоги 10', NULL, @i3, N'seed', @o3 OUTPUT;

    -- o1: повний цикл до Shipped
    EXEC dbo.usp_ChangeOrderStatus @o1, 'AwaitingValidation', N'seed';
    EXEC dbo.usp_ChangeOrderStatus @o1, 'StockConfirmed', N'seed';
    EXEC dbo.usp_ChangeOrderStatus @o1, 'Paid', N'seed';
    EXEC dbo.usp_ChangeOrderStatus @o1, 'Shipped', N'seed';
    -- o2: скасовано
    EXEC dbo.usp_ChangeOrderStatus @o2, 'Cancelled', N'seed';
    -- o3 залишається Submitted
END
GO
