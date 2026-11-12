using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>1:N з Product.</summary>
public class ProductImage
{
    private ProductImage() { }                              // для EF Core

    public ProductImage(string url, int sortOrder, bool isMain)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new DomainException("Image url is required");
        Url = url;
        SortOrder = sortOrder;
        IsMain = isMain;
    }

    public long Id { get; private set; }
    public long ProductId { get; private set; }
    public string Url { get; private set; } = "";
    public int SortOrder { get; private set; }
    public bool IsMain { get; private set; }

    public void SetMain(bool isMain) => IsMain = isMain;
}
