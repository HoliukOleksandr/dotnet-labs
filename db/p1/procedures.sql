-- ============================================================
-- OrdersDb: збережувані процедури (6 шт.)
-- Коди помилок: 50001 не знайдено; 50002 недопустимий перехід; 50003 конфлікт конкурентності;
--               50004 видалення заборонено; 50010 порожнє замовлення; 50011 клієнта немає;
--               50012 товару немає; 50013 недостатньо на складі
-- ============================================================
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE OrdersDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO

-- Тип для передачі позицій замовлення (Table-Valued Parameter; у Лаб. 02 — Dapper/ADO.NET DataTable)
IF TYPE_ID(N'dbo.OrderItemList') IS NULL
CREATE TYPE dbo.OrderItemList AS TABLE (
    ProductId BIGINT NOT NULL PRIMARY KEY,
    Units     INT    NOT NULL CHECK (Units > 0)
);
GO

-- 1) CRUD: створення клієнта
CREATE OR ALTER PROCEDURE dbo.usp_CreateCustomer
    @Email NVARCHAR(256), @FullName NVARCHAR(200), @CreatedBy NVARCHAR(100) = NULL,
    @CustomerId BIGINT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    INSERT dbo.Customers (Email, FullName, CreatedBy) VALUES (@Email, @FullName, @CreatedBy);
    SET @CustomerId = SCOPE_IDENTITY();
    SELECT @CustomerId AS CustomerId;
END
GO

-- 2) Бізнес-операція: створення замовлення (Orders + OrderDetails + OrderItems + історія) в одній транзакції
CREATE OR ALTER PROCEDURE dbo.usp_CreateOrder
    @CustomerId BIGINT, @ShippingAddress NVARCHAR(400), @Comment NVARCHAR(1000) = NULL,
    @Items dbo.OrderItemList READONLY, @CreatedBy NVARCHAR(100) = NULL,
    @OrderId BIGINT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM @Items) THROW 50010, 'Order must contain at least one item', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @CustomerId AND IsDeleted = 0) THROW 50011, 'Customer not found', 1;
    IF EXISTS (SELECT 1 FROM @Items i LEFT JOIN dbo.Products p ON p.Id = i.ProductId WHERE p.Id IS NULL)
        THROW 50012, 'Product not found in local catalog copy', 1;
    IF EXISTS (SELECT 1 FROM @Items i JOIN dbo.Products p WITH (UPDLOCK) ON p.Id = i.ProductId WHERE p.Stock < i.Units)
        THROW 50013, 'Not enough stock', 1;

    INSERT dbo.Orders (CustomerId, CreatedBy) VALUES (@CustomerId, @CreatedBy);
    SET @OrderId = SCOPE_IDENTITY();

    INSERT dbo.OrderDetails (OrderId, ShippingAddress, Comment) VALUES (@OrderId, @ShippingAddress, @Comment);

    -- знімок назви й ціни на момент покупки
    INSERT dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Units)
    SELECT @OrderId, p.Id, p.Name, p.Price, i.Units
    FROM @Items i JOIN dbo.Products p ON p.Id = i.ProductId;

    INSERT dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy)
    VALUES (@OrderId, NULL, 'Submitted', @CreatedBy);

    COMMIT TRANSACTION;
    SELECT @OrderId AS OrderId;
END
GO

-- 3) CRUD (read): замовлення з деталями та позиціями (2 result sets)
CREATE OR ALTER PROCEDURE dbo.usp_GetOrder @OrderId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE Id = @OrderId AND IsDeleted = 0) THROW 50001, 'Order not found', 1;

    SELECT o.Id, o.CustomerId, c.FullName, o.Status, o.OrderDate, d.ShippingAddress, d.Comment, o.RowVersion
    FROM dbo.Orders o
    JOIN dbo.Customers c    ON c.Id = o.CustomerId
    JOIN dbo.OrderDetails d ON d.OrderId = o.Id
    WHERE o.Id = @OrderId;

    SELECT ProductId, ProductName, UnitPrice, Discount, Units, (UnitPrice * Units - Discount) AS LineTotal
    FROM dbo.OrderItems WHERE OrderId = @OrderId;
END
GO

-- 4) CRUD (read): замовлення клієнта з фільтром за статусом і пагінацією
CREATE OR ALTER PROCEDURE dbo.usp_GetCustomerOrders
    @CustomerId BIGINT, @Status NVARCHAR(30) = NULL, @PageNumber INT = 1, @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT OrderId, Status, OrderDate, TotalAmount, TotalUnits
    FROM dbo.vw_OrderSummary
    WHERE CustomerId = @CustomerId AND (@Status IS NULL OR Status = @Status)
    ORDER BY OrderDate DESC, OrderId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- 5) ГОЛОВНА: транзакційна зміна статусу з перевіркою допустимого переходу
--    Submitted -> AwaitingValidation -> StockConfirmed -> Paid -> Shipped; Cancelled лише до оплати.
CREATE OR ALTER PROCEDURE dbo.usp_ChangeOrderStatus
    @OrderId BIGINT, @NewStatus NVARCHAR(30), @ChangedBy NVARCHAR(100) = NULL,
    @ExpectedRowVersion BINARY(8) = NULL            -- необов'язкова оптимістична конкурентність
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Old NVARCHAR(30), @Rv BINARY(8);
    SELECT @Old = Status, @Rv = RowVersion FROM dbo.Orders WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @OrderId AND IsDeleted = 0;

    IF @Old IS NULL THROW 50001, 'Order not found', 1;
    IF @ExpectedRowVersion IS NOT NULL AND @ExpectedRowVersion <> @Rv
        THROW 50003, 'Order was modified by another user (concurrency conflict)', 1;

    IF NOT EXISTS (
        SELECT 1 FROM (VALUES
            ('Submitted','AwaitingValidation'), ('Submitted','Cancelled'),
            ('AwaitingValidation','StockConfirmed'), ('AwaitingValidation','Cancelled'),
            ('StockConfirmed','Paid'), ('StockConfirmed','Cancelled'),
            ('Paid','Shipped')
        ) AS t(FromStatus, ToStatus)
        WHERE t.FromStatus = @Old AND t.ToStatus = @NewStatus)
    BEGIN
        DECLARE @Msg NVARCHAR(200) = CONCAT('Invalid status transition: ', @Old, ' -> ', @NewStatus);
        THROW 50002, @Msg, 1;
    END

    UPDATE dbo.Orders SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @ChangedBy WHERE Id = @OrderId;

    INSERT dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy) VALUES (@OrderId, @Old, @NewStatus, @ChangedBy);

    IF @NewStatus = 'Paid'
        INSERT dbo.PaymentRecords (OrderId, Amount)
        SELECT @OrderId, SUM(UnitPrice * Units - Discount) FROM dbo.OrderItems WHERE OrderId = @OrderId;

    COMMIT TRANSACTION;
END
GO

-- 6) CRUD (delete): м'яке видалення; дозволено лише для Submitted/Cancelled
CREATE OR ALTER PROCEDURE dbo.usp_DeleteOrder @OrderId BIGINT, @DeletedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @Status NVARCHAR(30) = (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);
    IF @Status IS NULL THROW 50001, 'Order not found', 1;
    IF @Status NOT IN ('Submitted','Cancelled') THROW 50004, 'Only Submitted or Cancelled orders can be deleted', 1;
    UPDATE dbo.Orders SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @DeletedBy WHERE Id = @OrderId;
    COMMIT TRANSACTION;
END
GO
