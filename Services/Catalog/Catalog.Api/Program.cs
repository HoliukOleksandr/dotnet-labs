using Catalog.Api.Middleware;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validators;
using Catalog.Dal;
using Catalog.Dal.Abstractions;
using Catalog.Dal.Seeding;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Рядок підключення: appsettings.json; перевизначається змінною оточення ConnectionStrings__CatalogDb (так передає Aspire)
var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing");

// Serilog: структуровані логи; SQL-команди EF Core на рівні Information (видно 1 запит для eager, 2 — для explicit)
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {RequestPath} {Message:lj}{NewLine}{Exception}"));

builder.Services.AddDbContext<CatalogDbContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    cfg.AddProfile<ProductProfile>();
    cfg.AddProfile<CategoryProfile>();
});

// FluentValidation: валідатори реєструються, а викликаються явно в контролері (AddFluentValidation застарів)
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDtoValidator>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

// властивості для всіх логів запиту: CorrelationId, UserId (якщо є), RequestPath
app.Use(async (ctx, next) =>
{
    using (LogContext.PushProperty("CorrelationId", ctx.TraceIdentifier))
    using (LogContext.PushProperty("UserId", ctx.User.Identity?.Name ?? "anonymous"))
    using (LogContext.PushProperty("RequestPath", ctx.Request.Path.Value))
    {
        await next();
    }
});
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

// Міграції + ідемпотентний програмний сідінг (вимикається в Production і налаштуваннями)
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();
    if (!app.Environment.IsProduction() && app.Configuration.GetValue("Seeding:Enabled", true))
        await DbInitializer.SeedAsync(db);
}

app.Run();

public partial class Program;
