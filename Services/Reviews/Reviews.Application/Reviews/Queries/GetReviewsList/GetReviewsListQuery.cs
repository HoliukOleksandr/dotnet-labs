using FluentValidation;
using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public record GetReviewsListQuery(long? ProductId, string? Search = null, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ReviewDto>>;

public class GetReviewsListQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewDto>>
{
    public Task<PagedResult<ReviewDto>> Handle(GetReviewsListQuery q, CancellationToken ct)
        => queries.GetListAsync(q.ProductId, q.Search, q.Page, Math.Clamp(q.PageSize, 1, 100), ct);
}

public class GetReviewsListQueryValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListQueryValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId.HasValue);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
