using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

/// <summary>Незмінний Value Object; у документі reviews зберігається як вкладений об'єкт "author".</summary>
public sealed class Author : ValueObject
{
    public string UserId { get; }
    public string DisplayName { get; }

    public Author(string userId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new DomainException("UserId is required");
        if (string.IsNullOrWhiteSpace(displayName)) throw new DomainException("DisplayName is required");
        UserId = userId.Trim();
        DisplayName = displayName.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return DisplayName;
    }
}
