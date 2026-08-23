using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Application.DTOs;

public record CreateOrderDto
{
    [Required] public int AddressId { get; init; }
    public int? ShippingMethodId { get; init; }
    public string PaymentMethod { get; init; } = "Cod";
    public string? CouponCode { get; init; }
    public string? Note { get; init; }
    /// <summary>Danh sách id dòng giỏ hàng muốn đặt. Rỗng = đặt toàn bộ giỏ.</summary>
    public List<int> CartItemIds { get; init; } = new();
    /// <summary>Số kỳ trả góp (6/9/12). Null = mua trả thẳng.</summary>
    public int? InstallmentMonths { get; init; }
}

public record OrderDto
{
    public int Id { get; init; }
    public string OrderCode { get; init; } = default!;
    public decimal SubTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = default!;
    public string ShippingAddress { get; init; } = default!;
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? PaymentMethod { get; init; }
    public string? PaymentStatus { get; init; }
    /// <summary>Số kỳ trả góp (null nếu trả thẳng).</summary>
    public int? InstallmentMonths { get; init; }
    /// <summary>Số tiền ước tính phải trả mỗi tháng (null nếu trả thẳng).</summary>
    public decimal? InstallmentMonthly { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public List<OrderStatusHistoryDto> StatusHistory { get; init; } = new();
    /// <summary>Các trạng thái kế tiếp hợp lệ theo quy trình (rỗng nếu đã kết thúc).</summary>
    public List<string> AllowedNextStatuses { get; init; } = new();
    /// <summary>Khách có được tự hủy đơn ở trạng thái hiện tại hay không.</summary>
    public bool CanCancelByCustomer { get; init; }
}

public record OrderItemDto
{
    public int Id { get; init; }
    public int VariantId { get; init; }
    public string ProductName { get; init; } = default!;
    public string? VariantInfo { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
    /// <summary>IMEI/Serial gán khi giao máy (null trước khi giao).</summary>
    public string? Imei { get; init; }
}

public record OrderStatusHistoryDto
{
    public string Status { get; init; } = default!;
    public string? Note { get; init; }
    public DateTime ChangedAt { get; init; }
}

public record UpdateOrderStatusDto
{
    [Required] public string Status { get; init; } = default!;
    public string? Note { get; init; }
    /// <summary>Khi chuyển sang Shipping: gán IMEI cho từng dòng máy để tạo phiếu bảo hành.</summary>
    public List<AssignImeiDto> ImeiAssignments { get; init; } = new();
}
