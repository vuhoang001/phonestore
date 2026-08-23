namespace PhoneStore.Domain.Entities;

/// <summary>Bảng nối nhiều-nhiều giữa Order và Coupon.</summary>
public class OrderCoupon
{
    public int OrderId { get; set; }
    public int CouponId { get; set; }
    /// <summary>Người dùng đã áp mã — phục vụ giới hạn mỗi người một lượt.</summary>
    public int UserId { get; set; }

    public Order Order { get; set; } = default!;
    public Coupon Coupon { get; set; } = default!;
}
