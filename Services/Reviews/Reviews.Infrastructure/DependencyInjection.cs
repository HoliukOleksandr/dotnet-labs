using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Interfaces;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;

namespace Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        MongoMappings.Register();                                       // один раз, до першого звернення до БД

        services.Configure<MongoDbOptions>(config.GetSection("MongoDb"));
        // Aspire передає рядок підключення як ConnectionStrings__ReviewsDb — він має пріоритет над appsettings
        services.PostConfigure<MongoDbOptions>(o =>
        {
            var fromAspire = config.GetConnectionString("ReviewsDb");
            if (!string.IsNullOrWhiteSpace(fromAspire)) o.ConnectionString = fromAspire;
        });

        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();
        services.AddSingleton<IDataSeeder, ReviewSeeder>();

        // порядок реєстрації = порядок запуску: спершу індекси, потім сідінг
        services.AddHostedService<IndexCreationService>();
        services.AddHostedService<DataSeedingService>();

        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb");
        return services;
    }
}
