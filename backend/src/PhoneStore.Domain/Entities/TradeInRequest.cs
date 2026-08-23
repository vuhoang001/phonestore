using PhoneStore.Domain.Common;
using PhoneStore.Domain.Enums;

namespace PhoneStore.Domain.Entities;

/// <summary>
/// Yêu cầu thu cũ đổi mới: khách khai máy cũ, admin định giá thu để trừ vào máy mới.
/// </summary>
public class TradeInRequest : BaseEntity
{
    public int UserId { get; set; }
    /// <summary>Model máy cũ khách khai (VD: iPhone 12 128GB).</summary>
    public string OldDeviceModel { get; set; } = default!;
    /// <summary>Tình trạng khai báo: Tốt / Trầy xước / Lỗi màn hình...</summary>
    public string Condition { get; set; } = default!;
    public string? Note { get; set; }
    /// <summary>Giá thu admin định (0 khi chưa báo giá).</summary>
    public decimal QuotedPrice { get; set; }
    public TradeInStatus Status { get; set; } = TradeInStatus.Pending;
    /// <summary>Máy mới khách muốn đổi sang (tùy chọn).</summary>
    public int? TargetProductId { get; set; }

    public User User { get; set; } = default!;
    public Product? TargetProduct { get; set; }
}
