using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Reviews.Application.Mapping;
using Reviews.Application.Reviews;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Tests;

public class MappingTests
{
    private static IMapper CreateMapper()
        => new MapperConfiguration(cfg => cfg.AddProfile<ReviewProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void AutoMapper_configuration_is_valid()
        => new MapperConfiguration(cfg => cfg.AddProfile<ReviewProfile>(), NullLoggerFactory.Instance)
            .AssertConfigurationIsValid();

    [Fact]
    public void Review_is_mapped_to_dto_with_string_id_and_comments_count()
    {
        var review = new Review(42, "Ноутбук X15", new Author("u-17", "Олена"), 5, "text");
        review.AddComment(new Author("u-33", "Андрій"), "Згоден");

        var dto = CreateMapper().Map<ReviewDto>(review);

        Assert.Equal(review.Id.ToString(), dto.Id);
        Assert.Equal("Олена", dto.AuthorName);
        Assert.Equal(1, dto.CommentsCount);
    }
}
