using Aspire.Hosting.ApplicationModel;

// Оркестрація Aspire: запускає сервіси разом із базами даних однією командою (dotnet run --project Platform.AppHost).
var builder = DistributedApplication.CreateBuilder(args);

// Імена ресурсів баз ("OrdersDb", "CatalogDb") збігаються з іменами рядків підключення в сервісах,
// тому Aspire передає їх автоматично (змінні ConnectionStrings__OrdersDb / ConnectionStrings__CatalogDb).
IResourceBuilder<IResourceWithConnectionString> ordersDb;
IResourceBuilder<IResourceWithConnectionString> catalogDb;

if (string.Equals(builder.Configuration["UseSqlServerContainer"], "true", StringComparison.OrdinalIgnoreCase))
{
    // Варіант 2: SQL Server у контейнері (потрібен Docker Desktop).
    // OrdersDb створюється порожньою: застосуйте db/p1/schema.sql, procedures.sql, seed.sql.
    // CatalogDb наповнюється сама: Catalog.Api застосовує міграції EF Core під час старту.
    var sql = builder.AddSqlServer("sql").WithDataVolume();
    ordersDb = sql.AddDatabase("OrdersDb");
    catalogDb = sql.AddDatabase("CatalogDb");
}
else
{
    // Варіант 1 (за замовчуванням): локальний SQL Server; рядки підключення в appsettings.json цього проєкту.
    ordersDb = builder.AddConnectionString("OrdersDb");
    catalogDb = builder.AddConnectionString("CatalogDb");
}

builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
