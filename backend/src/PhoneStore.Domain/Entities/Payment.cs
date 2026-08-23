using PhoneStore.Domain.Common;
using PhoneStore.Domain.Enums;

namespace PhoneStore.Domain.Entities;

/// <summary>Giao dịch thanh toán gắn với một đơn hàng.</summary>
public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionRef { get; set; }
    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = default!;
}
