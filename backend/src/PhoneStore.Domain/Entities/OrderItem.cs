using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Dòng hàng trong đơn. Snapshot tên/giá tại thời điểm đặt để bất biến.</summary>
public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int VariantId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public string? VariantInfoSnapshot { get; set; } // VD: "Đen · 256GB"
    public decimal PriceSnapshot { get; set; }
    public int Quantity { get; set; }
    /// <summary>IMEI/Serial gán cho máy này khi admin duyệt/giao (đặc thù điện thoại). Null trước khi giao.</summary>
    public string? Imei { get; set; }

    public Order Order { get; set; } = default!;
    public ProductVariant Variant { get; set; } = default!;
}
