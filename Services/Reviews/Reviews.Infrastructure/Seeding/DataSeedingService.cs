using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Reviews.Infrastructure.Seeding;

/// <summary>Запускає всі IDataSeeder під час старту (після створення індексів). Вимикається в Production або Seeding:Enabled=false.</summary>
public class DataSeedingService(
    IEnumerable<IDataSeeder> seeders,
    IConfiguration config,
    IHostEnvironment env,
    ILogger<DataSeedingService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (env.IsProduction() || !config.GetValue("Seeding:Enabled", true))
            return;

        try
        {
            foreach (var seeder in seeders)
                await seeder.SeedAsync(ct);
            logger.LogInformation("MongoDB seeding finished");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MongoDB seeding failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
