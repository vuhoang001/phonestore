namespace PhoneStore.Application.DTOs;

/// <summary>Một kỳ trả góp hiển thị cho khách. Status: Paid | Overdue | Pending (tính lúc đọc).</summary>
public record InstallmentPaymentDto(
    int Id, int InstallmentNo, DateTime DueDate, decimal Amount,
    string Status, DateTime? PaidAt, bool Payable);

/// <summary>Lịch trả góp của một đơn hàng (tổng hợp + danh sách kỳ).</summary>
public record InstallmentPlanDto
{
    public int OrderId { get; init; }
    public string OrderCode { get; init; } = default!;
    public DateTime OrderDate { get; init; }
    public decimal Total { get; init; }
    public int Months { get; init; }
    public decimal Monthly { get; init; }
    public int PaidCount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public DateTime? NextDueDate { get; init; }
    public bool Completed { get; init; }
    public List<InstallmentPaymentDto> Payments { get; init; } = new();
}
