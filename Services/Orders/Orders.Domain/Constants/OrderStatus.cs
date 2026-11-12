namespace Orders.Domain.Constants;

/// <summary>Статуси замовлення (як OrderStatus в eShop). Збігаються з CHECK у таблиці dbo.Orders.</summary>
public static class OrderStatus
{
    public const string Submitted = "Submitted";
    public const string AwaitingValidation = "AwaitingValidation";
    public const string StockConfirmed = "StockConfirmed";
    public const string Paid = "Paid";
    public const string Shipped = "Shipped";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Submitted, AwaitingValidation, StockConfirmed, Paid, Shipped, Cancelled
    };

    public static bool IsValid(string? status) => status is not null && All.Contains(status);
}
