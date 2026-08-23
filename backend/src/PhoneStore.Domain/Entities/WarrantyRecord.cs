using PhoneStore.Domain.Common;
using PhoneStore.Domain.Enums;

namespace PhoneStore.Domain.Entities;

/// <summary>
/// Phiếu bảo hành gắn theo IMEI/Serial của một máy đã bán (đặc thù điện thoại).
/// Tạo khi admin nhập IMEI lúc giao đơn. Khách tra cứu theo IMEI hoặc mã đơn.
/// </summary>
public class WarrantyRecord : BaseEntity
{
    public int OrderId { get; set; }
    public int OrderItemId { get; set; }
    public string Imei { get; set; } = default!;
    public string ProductNameSnapshot { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public WarrantyStatus Status { get; set; } = WarrantyStatus.Active;

    public Order Order { get; set; } = default!;
    public OrderItem OrderItem { get; set; } = default!;
}
