using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Domain.Interfaces;

/// <summary>Інтерфейс записів. Реалізація — в Infrastructure (MongoDB). Domain про драйвер нічого не знає.</summary>
public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(long productId, string authorId, CancellationToken ct = default);
    Task AddAsync(Review review, CancellationToken ct = default);
    /// <summary>Оновлення з перевіркою Version (optimistic concurrency); конфлікт -> ConflictException.</summary>
    Task UpdateAsync(Review review, CancellationToken ct = default);
    Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default);
}
