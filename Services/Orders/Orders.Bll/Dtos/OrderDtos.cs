using System.ComponentModel.DataAnnotations;

namespace Orders.Bll.Dtos;

public record OrderItemDto(long ProductId, string ProductName, decimal UnitPrice,
                           decimal Discount, int Units, decimal LineTotal);

public record OrderDto(long Id, long CustomerId, string Status, DateTime OrderDate,
                       string? ShippingAddress, string? Comment, decimal TotalAmount,
                       IReadOnlyList<OrderItemDto> Items);

public record OrderSummaryDto(long OrderId, string Status, DateTime OrderDate,
                              decimal TotalAmount, int TotalUnits);

public class CreateOrderItemDto
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; set; }

    [Range(1, 1000)]
    public int Units { get; set; }
}

public class CreateOrderDto
{
    [Range(1, long.MaxValue)]
    public long CustomerId { get; set; }

    [Required, StringLength(400, MinimumLength = 1)]
    public string ShippingAddress { get; set; } = "";

    [StringLength(1000)]
    public string? Comment { get; set; }

    [Required, MinLength(1)]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class UpdateOrderDetailsDto
{
    [Required, StringLength(400, MinimumLength = 1)]
    public string ShippingAddress { get; set; } = "";

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public class ChangeStatusDto
{
    [Required]
    public string Status { get; set; } = "";
}
