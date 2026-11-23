using Aspire.Hosting.ApplicationModel;

// Оркестрація Aspire: запускає сервіси разом із базами даних однією командою (dotnet run --project Platform.AppHost).
var builder = DistributedApplication.CreateBuilder(args);

// Імена ресурсів баз збігаються з іменами рядків підключення в сервісах,
// тому Aspire передає їх автоматично (ConnectionStrings__OrdersDb / CatalogDb / ReviewsDb).
IResourceBuilder<IResourceWithConnectionString> ordersDb;
IResourceBuilder<IResourceWithConnectionString> catalogDb;
IResourceBuilder<IResourceWithConnectionString> reviewsDb;

if (string.Equals(builder.Configuration["UseSqlServerContainer"], "true", StringComparison.OrdinalIgnoreCase))
{
    // Варіант 2: бази в контейнерах (потрібен Docker Desktop).
    // OrdersDb створюється порожньою: застосуйте db/p1/schema.sql, procedures.sql, seed.sql.
    // CatalogDb наповнюється сама (міграції EF Core), ReviewsDb — сама (індекси й сідінг при старті Reviews.WebApi).
    var sql = builder.AddSqlServer("sql").WithDataVolume();
    ordersDb = sql.AddDatabase("OrdersDb");
    catalogDb = sql.AddDatabase("CatalogDb");

    var mongo = builder.AddMongoDB("mongo").WithDataVolume();
    reviewsDb = mongo.AddDatabase("ReviewsDb");
}
else
{
    // Варіант 1 (за замовчуванням): локальні SQL Server і MongoDB; рядки підключення в appsettings.json цього проєкту.
    ordersDb = builder.AddConnectionString("OrdersDb");
    catalogDb = builder.AddConnectionString("CatalogDb");
    reviewsDb = builder.AddConnectionString("ReviewsDb");
}

builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
