using AutoMapper;
using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

/// <summary>ExpectedVersion (з If-Match/ETag) — необов'язкова: якщо вказана й не збігається з поточною, буде 409.</summary>
public record UpdateReviewCommand(string Id, int Rating, string? Text, int? ExpectedVersion = null)
    : ICommand<ReviewDto>;

public class UpdateReviewCommandHandler(IReviewRepository reviews, IMapper mapper)
    : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(UpdateReviewCommand cmd, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(ObjectId.Parse(cmd.Id), ct)
                     ?? throw new NotFoundException($"Review {cmd.Id} not found");

        if (cmd.ExpectedVersion is not null && cmd.ExpectedVersion != review.Version)
            throw new ConflictException($"Review {cmd.Id} was modified (current version {review.Version})");

        review.Edit(cmd.Rating, cmd.Text);
        await reviews.UpdateAsync(review, ct);        // перевірка Version на рівні MongoDB
        return mapper.Map<ReviewDto>(review);
    }
}

public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id).Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id must be a valid ObjectId");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Text).MaximumLength(2000);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0).When(x => x.ExpectedVersion.HasValue);
    }
}
