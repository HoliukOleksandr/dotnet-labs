# Опис предметної області та меж контекстів (Bounded Contexts)

Предметна область: **інтернет-магазин** (3 контексти, 3 незалежні БД, жодних FK між ними).

**Опис домену.** Покупці переглядають каталог товарів, оформлюють і оплачують замовлення та залишають відгуки й обговорюють товари. Система розділена на три автономні сервіси, кожен має власну БД.

| № | Контекст (Bounded Context) | БД | Технологія | Сутності |
|---|---|---|---|---|
| 1 | Замовлення | `OrdersDb` | SQL Server + ADO.NET/Dapper | Customers, Orders, OrderDetails, OrderItems, Products (локальна копія), OrderStatusHistory, PaymentRecords |
| 2 | Каталог | `CatalogDb` | SQL Server + EF Core | Products, ProductDetails, ProductImages, Categories, ProductCategories |
| 3 | Відгуки | `ReviewsDb` | MongoDB | reviews, discussions, discussion_messages, product_ratings |

Агрегати: **Order** = Orders + OrderDetails + OrderItems (+ історія); **Product** = Products + ProductDetails + ProductImages (+ зв'язки з категоріями); **Review** = review + вбудовані comments; **Discussion** = discussions + (reference) discussion_messages.
