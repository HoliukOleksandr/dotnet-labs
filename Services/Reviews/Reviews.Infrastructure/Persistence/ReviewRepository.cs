using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Infrastructure.Persistence;

public class ReviewRepository(MongoDbContext context) : IReviewRepository
{
    private readonly IMongoCollection<Review> _reviews = context.Reviews;

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(long productId, string authorId, CancellationToken ct = default)
        => _reviews.Find(r => r.ProductId == productId && r.Author.UserId == authorId).AnyAsync(ct);

    public Task AddAsync(Review review, CancellationToken ct = default)
        => _reviews.InsertOneAsync(review, cancellationToken: ct);   // дублікат -> MongoWriteException(DuplicateKey)

    /// <summary>
    /// Оптимістична конкурентність: оновлення застосовується лише до документа з очікуваною версією.
    /// Використовується UpdateOne ($set + $inc), а не ReplaceOne, щоб не затирати додаткові поля гнучкої схеми
    /// (pros, cons, videoUrl тощо), яких немає в доменній моделі.
    /// </summary>
    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var expectedVersion = review.Version;
        var f = Builders<Review>.Filter;

        // документи з Лаб. 01 можуть не мати поля version — це версія 0
        var versionFilter = expectedVersion == 0
            ? f.Or(f.Eq(r => r.Version, 0), f.Exists(r => r.Version, false))
            : f.Eq(r => r.Version, expectedVersion);

        var update = Builders<Review>.Update
            .Set(r => r.Rating, review.Rating)
            .Set(r => r.Text, review.Text)
            .Set("photos", review.Photos.ToList())
            .Set("comments", review.Comments.ToList())
            .Set(r => r.UpdatedAt, review.UpdatedAt)
            .Inc(r => r.Version, 1);

        var result = await _reviews.UpdateOneAsync(f.Eq(r => r.Id, review.Id) & versionFilter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)      // або документа немає, або хтось змінив його раніше
        {
            var exists = await _reviews.Find(r => r.Id == review.Id).AnyAsync(ct);
            throw exists
                ? new ConflictException($"Review {review.Id} was modified by someone else")
                : new NotFoundException($"Review {review.Id} not found");
        }

        review.BumpVersion();
    }

    public async Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default)
    {
        var result = await _reviews.DeleteOneAsync(r => r.Id == id, ct);
        return result.DeletedCount > 0;
    }
}
