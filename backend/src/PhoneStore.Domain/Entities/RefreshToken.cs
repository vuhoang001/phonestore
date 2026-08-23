using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Refresh token (lưu dạng hash) để cấp lại access token mà không cần đăng nhập lại.</summary>
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = default!;

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
