using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

/// <summary>
/// Створює індекси під час старту застосунку. Повторне створення наявного індексу безпечне — MongoDB його не дублює.
/// Індекси збігаються з тими, що створює db/p3/collections.js (Лаб. 01).
/// </summary>
public class IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var keys = Builders<Review>.IndexKeys;
            await context.Reviews.Indexes.CreateManyAsync(new[]
            {
                // «останні відгуки про товар»
                new CreateIndexModel<Review>(keys.Ascending(r => r.ProductId).Descending(r => r.CreatedAt)),
                // повнотекстовий пошук
                new CreateIndexModel<Review>(keys.Text(r => r.Text).Text(r => r.ProductName)),
                // один відгук від автора на товар (цілісність даних)
                new CreateIndexModel<Review>(
                    keys.Ascending(r => r.ProductId).Ascending("author.userId"),
                    new CreateIndexOptions { Unique = true })
            }, ct);

            logger.LogInformation("MongoDB indexes for 'reviews' are ready");
        }
        catch (Exception ex)
        {
            // застосунок має стартувати навіть якщо БД тимчасово недоступна: це видно в /health
            logger.LogError(ex, "Could not create MongoDB indexes");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
