using Reviews.Application.Common.Models;
using Reviews.Application.Reviews;

namespace Reviews.Application.Common.Interfaces;

/// <summary>
/// Інтерфейс читання (CQRS). Обробники запитів звертаються лише до нього, а не до IMongoCollection:
/// реалізація в Infrastructure використовує проєкції та aggregation pipeline.
/// </summary>
public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct);
    Task<PagedResult<ReviewDto>> GetListAsync(long? productId, string? search, int page, int pageSize, CancellationToken ct);
    /// <summary>Курсорна пагінація: наступні pageSize відгуків після відгуку з ідентифікатором after.</summary>
    Task<CursorPage<ReviewDto>> GetCursorPageAsync(long productId, string? after, int pageSize, CancellationToken ct);
}
