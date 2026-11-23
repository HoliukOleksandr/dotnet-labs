using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

/// <summary>Надає IMongoCollection&lt;T&gt; для доменних сутностей. Зареєстрований як Singleton (MongoClient потокобезпечний).</summary>
public class MongoDbContext
{
    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.ConnectionString))
            throw new InvalidOperationException("MongoDb:ConnectionString is not configured");
        if (string.IsNullOrWhiteSpace(o.DatabaseName))
            throw new InvalidOperationException("MongoDb:DatabaseName is not configured");

        var client = new MongoClient(o.ConnectionString);
        Database = client.GetDatabase(o.DatabaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<Review> Reviews => Database.GetCollection<Review>("reviews");
}
