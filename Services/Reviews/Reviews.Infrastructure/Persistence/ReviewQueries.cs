using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

/// <summary>
/// Читання (CQRS): aggregation pipeline з проєкцією одразу у ReviewDto — з БД вибираються лише потрібні поля.
/// Фільтрація, сортування, пагінація й підрахунок виконуються в MongoDB, а не в пам'яті.
/// </summary>
public class ReviewQueries(MongoDbContext context) : IReviewQueries
{
    // $project: лише потрібні поля + commentsCount ($size) і version (у старих документів її немає -> 0)
    private static readonly BsonDocument Projection = new()
    {
        { "productId", 1 },
        { "productName", 1 },
        { "author.displayName", 1 },
        { "rating", 1 },
        { "text", 1 },
        { "version", new BsonDocument("$ifNull", new BsonArray { "$version", 0 }) },
        { "commentsCount", new BsonDocument("$size",
            new BsonDocument("$ifNull", new BsonArray { "$comments", new BsonArray() })) }
    };

    public async Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        if (!ObjectId.TryParse(id, out var objectId)) return null;

        var docs = await context.Reviews.Aggregate()
            .Match(Builders<Review>.Filter.Eq(r => r.Id, objectId))
            .Project<BsonDocument>(Projection)
            .ToListAsync(ct);

        return docs.Count == 0 ? null : ToDto(docs[0]);
    }

    public async Task<PagedResult<ReviewDto>> GetListAsync(
        long? productId, string? search, int page, int pageSize, CancellationToken ct)
    {
        var filter = Builders<Review>.Filter.Empty;
        if (productId is not null)
            filter &= Builders<Review>.Filter.Eq(r => r.ProductId, productId.Value);      // $match
        if (!string.IsNullOrWhiteSpace(search))
            filter &= Builders<Review>.Filter.Text(search);                                // text-індекс

        var total = await context.Reviews.CountDocumentsAsync(filter, cancellationToken: ct);

        var docs = await context.Reviews.Aggregate()
            .Match(filter)
            .Sort(Builders<Review>.Sort.Descending(r => r.CreatedAt).Descending(r => r.Id))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<BsonDocument>(Projection)
            .ToListAsync(ct);

        return new PagedResult<ReviewDto>(docs.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<CursorPage<ReviewDto>> GetCursorPageAsync(
        long productId, string? after, int pageSize, CancellationToken ct)
    {
        var filter = Builders<Review>.Filter.Eq(r => r.ProductId, productId);
        if (!string.IsNullOrEmpty(after) && ObjectId.TryParse(after, out var lastId))
            filter &= Builders<Review>.Filter.Lt(r => r.Id, lastId);   // після останнього отриманого, без skip

        var docs = await context.Reviews.Aggregate()
            .Match(filter)
            .Sort(Builders<Review>.Sort.Descending(r => r.Id))          // ObjectId зростає разом із часом створення
            .Limit(pageSize + 1)                                        // +1, щоб дізнатися, чи є наступна сторінка
            .Project<BsonDocument>(Projection)
            .ToListAsync(ct);

        var items = docs.Take(pageSize).Select(ToDto).ToList();
        var next = docs.Count > pageSize && items.Count > 0 ? items[^1].Id : null;
        return new CursorPage<ReviewDto>(items, next);
    }

    private static ReviewDto ToDto(BsonDocument d)
    {
        var authorName = d.TryGetValue("author", out var author) && author.IsBsonDocument
            ? author.AsBsonDocument.GetValue("displayName", "").AsString
            : "";

        return new ReviewDto(
            Id: d["_id"].AsObjectId.ToString(),
            ProductId: d["productId"].ToInt64(),
            ProductName: d.GetValue("productName", "").AsString,
            AuthorName: authorName,
            Rating: d["rating"].ToInt32(),
            Text: d.GetValue("text", "").AsString,
            CommentsCount: d["commentsCount"].ToInt32(),
            Version: d["version"].ToInt32());
    }
}
