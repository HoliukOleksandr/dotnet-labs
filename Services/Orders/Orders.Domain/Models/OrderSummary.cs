namespace Orders.Domain.Models;

/// <summary>Рядок списку замовлень (результат usp_GetCustomerOrders).</summary>
public class OrderSummary
{
    public long OrderId { get; set; }
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public int? TotalUnits { get; set; }
}
