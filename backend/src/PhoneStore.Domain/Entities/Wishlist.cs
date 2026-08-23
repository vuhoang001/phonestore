using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Sản phẩm yêu thích của người dùng.</summary>
public class Wishlist : BaseEntity
{
    public int UserId { get; set; }
    public int ProductId { get; set; }

    public User User { get; set; } = default!;
    public Product Product { get; set; } = default!;
}
