using Reviews.Application;
using Reviews.Application.Mapping;
using Reviews.Infrastructure;
using Reviews.WebApi.Middleware;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

// кожен шар надає власний метод розширення
builder.Services
    .AddApplication(builder.Configuration)          // MediatR, Behaviors, валідатори
    .AddInfrastructure(builder.Configuration);      // MongoDB, репозиторії, індекси, сідінг, health check

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration["LuckyPenny:LicenseKey"];
    cfg.AddMaps(typeof(ReviewProfile).Assembly);
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (ctx, next) =>
{
    using (LogContext.PushProperty("CorrelationId", ctx.TraceIdentifier))
    {
        await next();
    }
});
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
