using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Địa chỉ nhận hàng của người dùng (hệ hành chính 2 cấp Tỉnh/Phường-Xã).</summary>
public class Address : BaseEntity
{
    public int UserId { get; set; }
    public string RecipientName { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Province { get; set; } = default!;
    public string District { get; set; } = ""; // Hệ 2 cấp không dùng; để rỗng cho tương thích
    public string Ward { get; set; } = default!;
    public string Detail { get; set; } = default!;      // Số nhà, tên đường
    public string? Note { get; set; }                    // Ghi chú giao hàng (tùy chọn)
    public bool IsDefault { get; set; }

    public User User { get; set; } = default!;
}
