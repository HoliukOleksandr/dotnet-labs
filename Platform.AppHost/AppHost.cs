using Aspire.Hosting.ApplicationModel;

// Оркестрація Aspire: запускає API разом із базою даних однією командою (dotnet run --project Platform.AppHost).
var builder = DistributedApplication.CreateBuilder(args);

// Ім'я ресурсу бази ("OrdersDb") збігається з ім'ям рядка підключення в Orders.Api,
// тому Aspire передає його в API автоматично (змінна ConnectionStrings__OrdersDb).
IResourceBuilder<IResourceWithConnectionString> ordersDb;

if (string.Equals(builder.Configuration["UseSqlServerContainer"], "true", StringComparison.OrdinalIgnoreCase))
{
    // Варіант 2: SQL Server у контейнері (потрібен Docker Desktop).
    // База створюється порожньою: застосуйте до неї db/p1/schema.sql, procedures.sql, seed.sql.
    ordersDb = builder.AddSqlServer("sql").WithDataVolume().AddDatabase("OrdersDb");
}
else
{
    // Варіант 1 (за замовчуванням): існуюча БД з Лаб. 01; рядок підключення в appsettings.json цього проєкту.
    ordersDb = builder.AddConnectionString("OrdersDb");
}

builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
