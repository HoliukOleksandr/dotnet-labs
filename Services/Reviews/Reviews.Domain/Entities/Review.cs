using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

/// <summary>Корінь агрегату «Відгук» (документ reviews з Лаб. 01). Інкапсуляція: приватні setter-и, зміни — методами.</summary>
public class Review : BaseEntity
{
    // не readonly: драйвер MongoDB має змогти заповнити поле під час десеріалізації
    private List<Comment> _comments = new();      // вкладений масив "comments"
    private List<string> _photos = new();         // необов'язковий масив "photos" (гнучка схема)

    public long ProductId { get; private set; }               // посилання на Каталог (без FK)
    public string ProductName { get; private set; } = "";     // денормалізовано для читання
    public Author Author { get; private set; } = null!;       // Value Object -> вкладений об'єкт
    public int Rating { get; private set; }
    public string Text { get; private set; } = "";
    public ObjectId? DiscussionId { get; private set; }       // reference на колекцію discussions

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<string> Photos => _photos.AsReadOnly();

    private Review() { }                                      // для десеріалізації

    public Review(long productId, string productName, Author author, int rating, string? text,
                  IEnumerable<string>? photos = null)
    {
        if (productId <= 0) throw new DomainException("ProductId must be positive");
        if (string.IsNullOrWhiteSpace(productName)) throw new DomainException("ProductName is required");
        if (rating is < 1 or > 5) throw new DomainException("Rating must be between 1 and 5");

        ProductId = productId;
        ProductName = productName.Trim();
        Author = author ?? throw new DomainException("Author is required");
        Rating = rating;
        Text = text?.Trim() ?? "";
        if (photos is not null) _photos = photos.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
    }

    public void Edit(int rating, string? text)
    {
        if (rating is < 1 or > 5) throw new DomainException("Rating must be between 1 and 5");
        Rating = rating;
        Text = text?.Trim() ?? "";
        Touch();
    }

    public void AddComment(Author author, string text)
    {
        if (author is null) throw new DomainException("Comment author is required");
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Comment text is required");
        _comments.Add(new Comment(author.DisplayName, text, DateTime.UtcNow));
        Touch();
    }
}
