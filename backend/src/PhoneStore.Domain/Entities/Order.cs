using PhoneStore.Domain.Common;
using PhoneStore.Domain.Enums;

namespace PhoneStore.Domain.Entities;

/// <summary>Đơn hàng của khách. OrderCode là mã public tránh lộ số thứ tự.</summary>
public class Order : BaseEntity
{
    public int UserId { get; set; }
    public string OrderCode { get; set; } = default!;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public int? AddressId { get; set; }
    public string ShippingAddressSnapshot { get; set; } = default!;
    public string? Note { get; set; }
    public string? CancelReason { get; set; } // lý do hủy (khi Status = Cancelled)

    // --- Trả góp (đặc thù điện thoại) ---
    /// <summary>Số kỳ trả góp (6/9/12 tháng). Null nếu mua trả thẳng.</summary>
    public int? InstallmentMonths { get; set; }
    /// <summary>Số tiền ước tính phải trả mỗi tháng (snapshot lúc đặt).</summary>
    public decimal? InstallmentMonthly { get; set; }

    public User User { get; set; } = default!;
    public Address? Address { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public Payment? Payment { get; set; }
    public ICollection<OrderCoupon> OrderCoupons { get; set; } = new List<OrderCoupon>();
}
