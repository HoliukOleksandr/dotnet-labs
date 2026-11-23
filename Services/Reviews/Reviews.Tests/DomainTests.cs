using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Tests;

public class DomainTests
{
    private static readonly Author Olena = new("u-17", "Олена");

    [Fact]
    public void Review_with_rating_10_throws_DomainException_without_any_database()
        => Assert.Throws<DomainException>(() => new Review(42, "Ноутбук X15", Olena, 10, "text"));

    [Fact]
    public void Author_is_compared_by_value()
    {
        Assert.Equal(new Author("u-17", "Олена"), new Author("u-17", "Олена"));
        Assert.NotEqual(new Author("u-17", "Олена"), new Author("u-18", "Олена"));
    }

    [Fact]
    public void AddComment_adds_nested_comment_and_touches_entity()
    {
        var review = new Review(42, "Ноутбук X15", Olena, 5, "text");

        review.AddComment(new Author("u-33", "Андрій"), "Згоден");

        Assert.Single(review.Comments);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void AddComment_with_empty_text_throws()
    {
        var review = new Review(42, "Ноутбук X15", Olena, 5, "text");
        Assert.Throws<DomainException>(() => review.AddComment(Olena, " "));
    }
}
