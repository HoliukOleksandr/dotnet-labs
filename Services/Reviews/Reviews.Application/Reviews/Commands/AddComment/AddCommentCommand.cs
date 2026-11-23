using AutoMapper;
using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.AddComment;

public record AddCommentCommand(string ReviewId, string AuthorId, string AuthorName, string Text, int? ExpectedVersion = null)
    : ICommand<ReviewDto>;

public class AddCommentCommandHandler(IReviewRepository reviews, IMapper mapper)
    : IRequestHandler<AddCommentCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(AddCommentCommand cmd, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(ObjectId.Parse(cmd.ReviewId), ct)
                     ?? throw new NotFoundException($"Review {cmd.ReviewId} not found");

        if (cmd.ExpectedVersion is not null && cmd.ExpectedVersion != review.Version)
            throw new ConflictException($"Review {cmd.ReviewId} was modified (current version {review.Version})");

        review.AddComment(new Author(cmd.AuthorId, cmd.AuthorName), cmd.Text);
        await reviews.UpdateAsync(review, ct);
        return mapper.Map<ReviewDto>(review);
    }
}

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.ReviewId).Must(id => ObjectId.TryParse(id, out _)).WithMessage("ReviewId must be a valid ObjectId");
        RuleFor(x => x.AuthorId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000);
    }
}
