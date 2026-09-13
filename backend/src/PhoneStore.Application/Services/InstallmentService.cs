using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

/// <summary>
/// Trả góp hàng tháng (mock 0% lãi). Khách xem lịch trả của từng đơn và thanh toán
/// tuần tự từng kỳ. Trả hết mọi kỳ → đánh dấu thanh toán đơn hoàn tất.
/// </summary>
public class InstallmentService : IInstallmentService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notif;
    public InstallmentService(IAppDbContext db, INotificationService notif)
    {
        _db = db;
        _notif = notif;
    }

    public async Task<IReadOnlyList<InstallmentPlanDto>> GetMyPlansAsync(int userId)
    {
        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId && o.InstallmentMonths != null)
            .Include(o => o.InstallmentPayments)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return orders.Select(BuildPlan).ToList();
    }

    public async Task<InstallmentPlanDto> PayAsync(int userId, int installmentId)
    {
        var inst = await _db.InstallmentPayments
            .Include(i => i.Order).ThenInclude(o => o.InstallmentPayments)
            .Include(i => i.Order).ThenInclude(o => o.Payment)
            .FirstOrDefaultAsync(i => i.Id == installmentId);

        if (inst is null || inst.Order.UserId != userId)
            throw new AppException("Không tìm thấy kỳ trả góp.");
        if (inst.Status == InstallmentStatus.Paid)
            throw new AppException("Kỳ này đã được thanh toán.");

        // Phải trả tuần tự: kỳ sớm nhất chưa trả trước.
        var earliest = inst.Order.InstallmentPayments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .OrderBy(i => i.InstallmentNo)
            .First();
        if (earliest.Id != inst.Id)
            throw new AppException($"Vui lòng thanh toán kỳ {earliest.InstallmentNo} trước.");

        inst.Status = InstallmentStatus.Paid;
        inst.PaidAt = DateTime.UtcNow;

        // Trả hết tất cả kỳ → thanh toán đơn hoàn tất.
        var allPaid = inst.Order.InstallmentPayments.All(i => i.Status == InstallmentStatus.Paid);
        if (allPaid && inst.Order.Payment is not null && inst.Order.Payment.Status != PaymentStatus.Paid)
        {
            inst.Order.Payment.Status = PaymentStatus.Paid;
            inst.Order.Payment.PaidAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _notif.NotifyUserAsync(userId,
            allPaid ? "Hoàn tất trả góp" : "Đã thu kỳ trả góp",
            allPaid
                ? $"Bạn đã trả xong toàn bộ {inst.Order.InstallmentPayments.Count} kỳ của đơn {inst.Order.OrderCode}."
                : $"Đã thanh toán kỳ {inst.InstallmentNo} ({inst.Amount:N0}đ) của đơn {inst.Order.OrderCode}.",
            "order", $"/installments");

        return BuildPlan(inst.Order);
    }

    /// <summary>Gom lịch + tổng hợp cho 1 đơn. Status kỳ tính lúc đọc: Paid | Overdue | Pending.</summary>
    private static InstallmentPlanDto BuildPlan(Order o)
    {
        var pays = o.InstallmentPayments.OrderBy(i => i.InstallmentNo).ToList();
        var today = DateTime.UtcNow.Date;

        var earliestUnpaidNo = pays
            .Where(i => i.Status != InstallmentStatus.Paid)
            .OrderBy(i => i.InstallmentNo)
            .Select(i => (int?)i.InstallmentNo)
            .FirstOrDefault();

        static string Disp(InstallmentPayment i, DateTime today) =>
            i.Status == InstallmentStatus.Paid ? "Paid"
            : i.DueDate.Date < today ? "Overdue"
            : "Pending";

        return new InstallmentPlanDto
        {
            OrderId = o.Id,
            OrderCode = o.OrderCode,
            OrderDate = o.CreatedAt,
            Total = o.TotalAmount,
            Months = o.InstallmentMonths ?? pays.Count,
            Monthly = o.InstallmentMonthly ?? 0,
            PaidCount = pays.Count(i => i.Status == InstallmentStatus.Paid),
            PaidAmount = pays.Where(i => i.Status == InstallmentStatus.Paid).Sum(i => i.Amount),
            RemainingAmount = pays.Where(i => i.Status != InstallmentStatus.Paid).Sum(i => i.Amount),
            NextDueDate = pays.Where(i => i.Status != InstallmentStatus.Paid)
                .OrderBy(i => i.InstallmentNo).Select(i => (DateTime?)i.DueDate).FirstOrDefault(),
            Completed = pays.Count > 0 && pays.All(i => i.Status == InstallmentStatus.Paid),
            Payments = pays.Select(i => new InstallmentPaymentDto(
                i.Id, i.InstallmentNo, i.DueDate, i.Amount, Disp(i, today), i.PaidAt,
                i.InstallmentNo == earliestUnpaidNo)).ToList()
        };
    }
}
