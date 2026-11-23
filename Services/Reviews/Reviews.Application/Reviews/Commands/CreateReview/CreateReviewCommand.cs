using FluentValidation;
using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public record CreateReviewCommand(long ProductId, string ProductName, string AuthorId, string AuthorName,
                                  int Rating, string? Text, IReadOnlyList<string>? Photos = null)
    : ICommand<string>;

public class CreateReviewCommandHandler(IReviewRepository reviews)
    : IRequestHandler<CreateReviewCommand, string>
{
    public async Task<string> Handle(CreateReviewCommand cmd, CancellationToken ct)
    {
        var review = new Review(cmd.ProductId, cmd.ProductName,
            new Author(cmd.AuthorId, cmd.AuthorName), cmd.Rating, cmd.Text, cmd.Photos);
        await reviews.AddAsync(review, ct);
        return review.Id.ToString();
    }
}

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator(IReviewRepository reviews)
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AuthorId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Text).MaximumLength(2000);

        // асинхронна валідація із запитом до MongoDB: один відгук від автора на товар
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) => !await reviews.ExistsAsync(cmd.ProductId, cmd.AuthorId, ct))
            .When(x => x.ProductId > 0 && !string.IsNullOrWhiteSpace(x.AuthorId))
            .WithName("Review")
            .WithMessage("You have already reviewed this product");
    }
}
