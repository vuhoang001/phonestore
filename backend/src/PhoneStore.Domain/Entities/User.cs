using PhoneStore.Domain.Common;
using PhoneStore.Domain.Enums;

namespace PhoneStore.Domain.Entities;

/// <summary>Tài khoản người dùng (khách hàng / admin).</summary>
public class User : BaseEntity
{
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? Phone { get; set; }
    /// <summary>Ảnh đại diện (URL trên MinIO). Null nếu chưa đặt → hiển thị chữ cái đầu.</summary>
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsActive { get; set; } = true;
    /// <summary>Email đã được xác nhận qua link gửi về hòm thư.</summary>
    public bool EmailConfirmed { get; set; }

    // Navigation
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public Cart? Cart { get; set; }
}
