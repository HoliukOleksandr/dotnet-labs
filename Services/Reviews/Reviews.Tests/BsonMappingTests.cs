using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Tests;

/// <summary>Документ має структуру з Лаб. 01 (camelCase, вкладені author і comments) без витоку внутрішніх полів .NET.</summary>
public class BsonMappingTests
{
    private static Review Sample()
    {
        var review = new Review(42, "Ноутбук X15", new Author("u-17", "Олена"), 5, "Чудовий екран", new[] { "img1.jpg" });
        review.AddComment(new Author("u-33", "Андрій"), "Згоден");
        return review;
    }

    [Fact]
    public void Document_has_lab01_structure_without_private_fields()
    {
        MongoMappings.Register();

        var doc = Sample().ToBsonDocument();

        foreach (var field in new[] { "_id", "productId", "productName", "author", "rating", "text", "photos", "comments", "createdAt", "version" })
            Assert.True(doc.Contains(field), $"missing field {field}");

        Assert.False(doc.Contains("_comments"));
        Assert.False(doc.Contains("_photos"));
        Assert.False(doc.Contains("Comments"));
        Assert.Equal("u-17", doc["author"]["userId"].AsString);
        Assert.Equal("Олена", doc["author"]["displayName"].AsString);
        Assert.Equal("Андрій", doc["comments"][0]["author"].AsString);
    }

    [Fact]
    public void Document_round_trips_through_bson()
    {
        MongoMappings.Register();
        var review = Sample();

        var back = BsonSerializer.Deserialize<Review>(review.ToBsonDocument());

        Assert.Equal(review.Id, back.Id);
        Assert.Equal(review.Author, back.Author);
        Assert.Equal(5, back.Rating);
        Assert.Single(back.Comments);
        Assert.Single(back.Photos);
    }
}
