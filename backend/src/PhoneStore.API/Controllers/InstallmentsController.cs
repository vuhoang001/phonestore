using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

/// <summary>Trả góp hàng tháng của khách: xem lịch trả & thanh toán từng kỳ (mock).</summary>
[Authorize]
[Route("api/installments")]
public class InstallmentsController : BaseApiController
{
    private readonly IInstallmentService _service;
    public InstallmentsController(IInstallmentService service) => _service = service;

    /// <summary>Lịch trả góp của tất cả đơn trả góp của tôi.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<InstallmentPlanDto>>> Mine()
        => Ok(await _service.GetMyPlansAsync(CurrentUserId));

    /// <summary>Thanh toán một kỳ trả góp (mock). Trả về lịch cập nhật của đơn đó.</summary>
    [HttpPost("{id:int}/pay")]
    public async Task<ActionResult<InstallmentPlanDto>> Pay(int id)
        => Ok(await _service.PayAsync(CurrentUserId, id));
}
