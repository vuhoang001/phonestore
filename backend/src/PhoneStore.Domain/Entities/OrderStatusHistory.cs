using PhoneStore.Domain.Common;
using PhoneStore.Domain.Enums;

namespace PhoneStore.Domain.Entities;

/// <summary>Lịch sử thay đổi trạng thái đơn hàng.</summary>
public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }

    public Order Order { get; set; } = default!;
}
