using PhoneStore.Domain.Common;
using PhoneStore.Domain.Enums;

namespace PhoneStore.Domain.Entities;

/// <summary>Token dùng một lần: xác nhận email hoặc đặt lại mật khẩu (lưu dạng hash).</summary>
public class UserToken : BaseEntity
{
    public int UserId { get; set; }
    public UserTokenPurpose Purpose { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public User User { get; set; } = default!;

    public bool IsUsable => UsedAt is null && DateTime.UtcNow < ExpiresAt;
}
