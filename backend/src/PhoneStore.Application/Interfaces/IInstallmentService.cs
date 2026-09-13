using PhoneStore.Application.DTOs;

namespace PhoneStore.Application.Interfaces;

/// <summary>Trả góp hàng tháng (mock 0% lãi): khách xem lịch trả và thanh toán từng kỳ.</summary>
public interface IInstallmentService
{
    /// <summary>Lịch trả góp của tất cả đơn trả góp của khách.</summary>
    Task<IReadOnlyList<InstallmentPlanDto>> GetMyPlansAsync(int userId);

    /// <summary>Thanh toán 1 kỳ (mock). Phải trả tuần tự — kỳ sớm nhất chưa trả trước.</summary>
    Task<InstallmentPlanDto> PayAsync(int userId, int installmentId);
}
