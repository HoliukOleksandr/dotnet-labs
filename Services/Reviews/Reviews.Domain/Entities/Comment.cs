using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.Entities;

/// <summary>Вкладений коментар (елемент масиву "comments"): незмінний, порівнюється за значеннями.</summary>
public sealed class Comment : ValueObject
{
    public string Author { get; }
    public string Text { get; }
    public DateTime CreatedAt { get; }

    public Comment(string author, string text, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(author)) throw new DomainException("Comment author is required");
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Comment text is required");
        Author = author.Trim();
        Text = text.Trim();
        CreatedAt = createdAt;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Author;
        yield return Text;
        yield return CreatedAt;
    }
}
