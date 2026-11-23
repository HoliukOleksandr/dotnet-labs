namespace Orders.Domain.Models;

/// <summary>Локальна копія довідника Каталогу (власник даних — CatalogDb).</summary>
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
