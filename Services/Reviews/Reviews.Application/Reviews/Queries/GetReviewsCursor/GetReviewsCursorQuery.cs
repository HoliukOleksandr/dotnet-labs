using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Queries.GetReviewsCursor;

/// <summary>Курсорна пагінація (додатково): наступна сторінка починається ПІСЛЯ останнього отриманого документа, без skip.</summary>
public record GetReviewsCursorQuery(long ProductId, string? After = null, int PageSize = 20)
    : IQuery<CursorPage<ReviewDto>>;

public class GetReviewsCursorQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewsCursorQuery, CursorPage<ReviewDto>>
{
    public Task<CursorPage<ReviewDto>> Handle(GetReviewsCursorQuery q, CancellationToken ct)
        => queries.GetCursorPageAsync(q.ProductId, q.After, Math.Clamp(q.PageSize, 1, 100), ct);
}

public class GetReviewsCursorQueryValidator : AbstractValidator<GetReviewsCursorQuery>
{
    public GetReviewsCursorQueryValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.After).Must(a => ObjectId.TryParse(a, out _))
            .When(x => !string.IsNullOrEmpty(x.After))
            .WithMessage("After must be a valid ObjectId");
    }
}
