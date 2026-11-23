using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Reviews.Domain.Common;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence;

/// <summary>
/// BSON-мапінг налаштовується тут (BsonClassMap), а не атрибутами в Domain: домен не залежить від деталей зберігання.
/// Реєструється один раз — під час старту, до першого звернення до БД.
/// </summary>
public static class MongoMappings
{
    private static readonly object Lock = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Lock)
        {
            if (_registered) return;

            // єдина конвенція імен (camelCase, як у документі з Лаб. 01) + ігнорування невідомих полів гнучкої схеми
            ConventionRegistry.Register("reviews-conventions",
                new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new IgnoreExtraElementsConvention(true)
                },
                _ => true);

            BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
            {
                cm.MapIdProperty(e => e.Id);                              // ObjectId -> _id
                cm.MapProperty(e => e.CreatedAt);
                cm.MapProperty(e => e.UpdatedAt).SetIgnoreIfNull(true);
                cm.MapProperty(e => e.Version);
            });

            BsonClassMap.RegisterClassMap<Review>(cm =>
            {
                cm.MapProperty(r => r.ProductId);
                cm.MapProperty(r => r.ProductName);
                cm.MapProperty(r => r.Author);                            // вкладений Value Object
                cm.MapProperty(r => r.Rating);
                cm.MapProperty(r => r.Text);
                cm.MapProperty(r => r.DiscussionId).SetIgnoreIfNull(true); // у частини документів поля немає
                cm.MapField("_comments").SetElementName("comments");      // приватне поле -> масив "comments"
                cm.MapField("_photos").SetElementName("photos");          // приватне поле -> масив "photos"
            });

            // незмінні Value Objects створюються через конструктор
            BsonClassMap.RegisterClassMap<Author>(cm =>
            {
                cm.MapProperty(a => a.UserId);
                cm.MapProperty(a => a.DisplayName);
                cm.MapCreator(a => new Author(a.UserId, a.DisplayName));
            });

            BsonClassMap.RegisterClassMap<Comment>(cm =>
            {
                cm.MapProperty(c => c.Author);
                cm.MapProperty(c => c.Text);
                cm.MapProperty(c => c.CreatedAt);
                cm.MapCreator(c => new Comment(c.Author, c.Text, c.CreatedAt));
            });

            _registered = true;
        }
    }
}
