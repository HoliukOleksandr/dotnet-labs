using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

/// <summary>Ідемпотентний сідінг: якщо в колекції вже є документи (напр. з Лаб. 01), нічого не додається.</summary>
public class ReviewSeeder(MongoDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.Reviews.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
            return;                                              // уже наповнено

        // productId збігаються з товарами Каталогу; структура — як у документі reviews з Лаб. 01
        var laptop = new Review(1, "Ноутбук X15", new Author("u-17", "Олена"), 5, "Чудовий екран",
                                new[] { "img1.jpg", "img2.jpg" });
        laptop.AddComment(new Author("u-33", "Андрій"), "Згоден");

        var reviews = new[]
        {
            laptop,
            new Review(1, "Ноутбук X15", new Author("u-21", "Іван"), 4, "Швидкий, але гарячий"),
            new Review(5, "Навушники H9", new Author("u-17", "Олена"), 3, "Звук непоганий"),
            new Review(2, "Бездротова миша M200", new Author("u-33", "Максим"), 5, null)
        };

        await context.Reviews.InsertManyAsync(reviews, cancellationToken: ct);   // bulk-вставка
    }
}
