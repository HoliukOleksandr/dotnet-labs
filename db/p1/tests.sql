-- OrdersDb: запити перевірки (CRUD, пошук, пагінація, транзакції, обмеження)
-- Скрипт можна запускати повторно: кожного разу створюється нове тестове замовлення.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE OrdersDb;
GO
-- 1. Пагінація та фільтр за статусом
EXEC dbo.usp_GetCustomerOrders @CustomerId = 1, @Status = NULL, @PageNumber = 1, @PageSize = 5;
EXEC dbo.usp_GetCustomerOrders @CustomerId = 1, @Status = 'Shipped';
-- 2. Читання замовлення (2 result sets)
EXEC dbo.usp_GetOrder @OrderId = 1;
GO
-- 3. Нове тестове замовлення і допустимий перехід
DECLARE @items dbo.OrderItemList, @o BIGINT;
INSERT @items VALUES (2, 1);
EXEC dbo.usp_CreateOrder @CustomerId = 1, @ShippingAddress = N'Тестова адреса', @Comment = NULL,
                         @Items = @items, @CreatedBy = N'tester', @OrderId = @o OUTPUT;
EXEC dbo.usp_ChangeOrderStatus @o, 'AwaitingValidation', N'tester';

-- 4. НЕДОПУСТИМИЙ перехід: відвантажити неоплачене -> керована помилка 50002
BEGIN TRY
    EXEC dbo.usp_ChangeOrderStatus @o, 'Shipped', N'tester';
    PRINT 'ПОМИЛКА: перехід мав бути відхилений!';
END TRY
BEGIN CATCH
    PRINT CONCAT('OK, відхилено: ', ERROR_NUMBER(), ' - ', ERROR_MESSAGE());
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;   -- у тесті прибираємо "зламану" транзакцію
END CATCH;

-- 5. Скасувати оплачене (відвантажене) замовлення не можна
BEGIN TRY
    EXEC dbo.usp_ChangeOrderStatus @OrderId = 1, @NewStatus = 'Cancelled';
    PRINT 'ПОМИЛКА: перехід мав бути відхилений!';
END TRY
BEGIN CATCH
    PRINT CONCAT('OK, відхилено: ', ERROR_NUMBER(), ' - ', ERROR_MESSAGE());
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;

-- 6. CHECK-обмеження: від'ємна кількість
BEGIN TRY
    INSERT dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Units) VALUES (3, 1, N'x', 10, -1);
    PRINT 'ПОМИЛКА: CHECK не спрацював!';
END TRY
BEGIN CATCH
    PRINT CONCAT('OK, CHECK спрацював: ', ERROR_MESSAGE());
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;
GO
-- 7. CRUD: створення клієнта (Create), читання (Read), м'яке видалення замовлення (Delete)
DECLARE @cid BIGINT, @email NVARCHAR(256) = CONCAT(N'test_', CONVERT(NVARCHAR(36), NEWID()), N'@example.com');
EXEC dbo.usp_CreateCustomer @Email = @email, @FullName = N'Тест Клієнт', @CreatedBy = N'tester', @CustomerId = @cid OUTPUT;
SELECT * FROM dbo.Customers WHERE Id = @cid;

DECLARE @items2 dbo.OrderItemList, @o2 BIGINT;
INSERT @items2 VALUES (3, 1);
EXEC dbo.usp_CreateOrder @CustomerId = @cid, @ShippingAddress = N'Тестова адреса 2', @Comment = NULL,
                         @Items = @items2, @CreatedBy = N'tester', @OrderId = @o2 OUTPUT;
EXEC dbo.usp_DeleteOrder @OrderId = @o2, @DeletedBy = N'tester';
SELECT Id, Status, IsDeleted FROM dbo.Orders WHERE Id = @o2;     -- IsDeleted = 1

-- Видалити відвантажене замовлення не можна (керована помилка 50004)
BEGIN TRY
    EXEC dbo.usp_DeleteOrder @OrderId = 1;
    PRINT 'ПОМИЛКА: видалення мало бути відхилене!';
END TRY
BEGIN CATCH
    PRINT CONCAT('OK, відхилено: ', ERROR_NUMBER(), ' - ', ERROR_MESSAGE());
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;
GO
-- 8. Історія статусів
SELECT * FROM dbo.OrderStatusHistory ORDER BY OrderId, ChangedAt;
-- 9. Оплати
SELECT * FROM dbo.PaymentRecords;
-- 10. САМОПЕРЕВІРКА: всі FK ведуть лише на таблиці цієї ж БД (жодних міжбазових)
SELECT fk.name AS FkName, OBJECT_NAME(fk.parent_object_id) AS ChildTable,
       OBJECT_NAME(fk.referenced_object_id) AS ParentTable
FROM sys.foreign_keys fk;
