using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.DeleteReview;

public record DeleteReviewCommand(string Id) : ICommand;

public class DeleteReviewCommandHandler(IReviewRepository reviews) : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand cmd, CancellationToken ct)
    {
        if (!await reviews.DeleteAsync(ObjectId.Parse(cmd.Id), ct))
            throw new NotFoundException($"Review {cmd.Id} not found");
    }
}

public class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
        => RuleFor(x => x.Id).Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id must be a valid ObjectId");
}
