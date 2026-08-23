using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Giỏ hàng: của người dùng đã đăng nhập (UserId) hoặc của khách vãng lai (GuestToken).</summary>
public class Cart : BaseEntity
{
    public int? UserId { get; set; }
    /// <summary>Định danh giỏ khách chưa đăng nhập (lưu ở localStorage phía client).</summary>
    public string? GuestToken { get; set; }

    public User? User { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
