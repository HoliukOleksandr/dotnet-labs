using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Reviews.Application;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Tests;

/// <summary>Перевіряє повний ланцюжок MediatR: Logging -> Validation -> Performance -> обробник.</summary>
public class PipelineTests
{
    private static (ISender Sender, IReviewRepository Repo, ServiceProvider Provider) Build()
    {
        var repo = Substitute.For<IReviewRepository>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(new ConfigurationBuilder().Build());
        services.AddSingleton(repo);
        var provider = services.BuildServiceProvider();
        return (provider.CreateScope().ServiceProvider.GetRequiredService<ISender>(), repo, provider);
    }

    [Fact]
    public async Task Command_with_rating_10_is_rejected_before_handler()
    {
        var (sender, repo, provider) = Build();
        using var _ = provider;

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new CreateReviewCommand(1, "Ноутбук X15", "u-17", "Олена", 10, "text")));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateReviewCommand.Rating));
        await repo.DidNotReceive().AddAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_review_by_same_author_is_rejected_by_async_validator()
    {
        var (sender, repo, provider) = Build();
        using var _ = provider;
        repo.ExistsAsync(1, "u-17", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new CreateReviewCommand(1, "Ноутбук X15", "u-17", "Олена", 5, "text")));

        await repo.DidNotReceive().AddAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Valid_command_reaches_handler_and_returns_object_id()
    {
        var (sender, repo, provider) = Build();
        using var _ = provider;

        var id = await sender.Send(new CreateReviewCommand(1, "Ноутбук X15", "u-17", "Олена", 5, "text"));

        Assert.Equal(24, id.Length);
        await repo.Received(1).AddAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }
}
