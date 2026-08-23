using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Thông báo gửi tới người dùng (đơn hàng, khuyến mãi, bảo hành...).</summary>
public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Type { get; set; } = "order"; // order | promotion | system | warranty
    public string? Link { get; set; }
    public bool IsRead { get; set; }

    public User User { get; set; } = default!;
}
