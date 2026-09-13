using PhoneStore.Domain.Common;
using PhoneStore.Domain.Enums;

namespace PhoneStore.Domain.Entities;

/// <summary>
/// Một kỳ trả góp hàng tháng của đơn hàng (lịch trả). Sinh tự động khi đặt đơn trả góp.
/// Khách thanh toán tuần tự từng kỳ (mock 0% lãi).
/// </summary>
public class InstallmentPayment : BaseEntity
{
    public int OrderId { get; set; }
    /// <summary>Số thứ tự kỳ (1..N).</summary>
    public int InstallmentNo { get; set; }
    /// <summary>Ngày đến hạn của kỳ này.</summary>
    public DateTime DueDate { get; set; }
    /// <summary>Số tiền phải trả kỳ này.</summary>
    public decimal Amount { get; set; }
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;
    /// <summary>Thời điểm khách trả kỳ này (null nếu chưa trả).</summary>
    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = default!;
}
