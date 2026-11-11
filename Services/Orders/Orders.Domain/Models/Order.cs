using Orders.Domain.Constants;

namespace Orders.Domain.Models;

/// <summary>Агрегат «Замовлення»: Orders + OrderDetails (1:1) + OrderItems (M:N з Products).</summary>
public class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = OrderStatus.Submitted;
    public DateTime OrderDate { get; set; }
    public string? ShippingAddress { get; set; }   // OrderDetails
    public string? Comment { get; set; }           // OrderDetails
    public List<OrderItem> Items { get; } = new();

    public decimal TotalAmount => Items.Sum(i => i.LineTotal);
}
