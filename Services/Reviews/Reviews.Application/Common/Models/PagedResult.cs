namespace Reviews.Application.Common.Models;

public record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

/// <summary>Сторінка курсорної пагінації: NextCursor == null означає, що це остання сторінка.</summary>
public record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor);
