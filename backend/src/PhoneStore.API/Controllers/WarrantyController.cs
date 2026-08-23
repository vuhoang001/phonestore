using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

/// <summary>Bảo hành theo IMEI (đặc thù điện thoại): tra cứu công khai, gán IMEI cho admin.</summary>
[Route("api/warranty")]
public class WarrantyController : BaseApiController
{
    private readonly IWarrantyService _service;
    public WarrantyController(IWarrantyService service) => _service = service;

    /// <summary>Tra cứu bảo hành theo IMEI hoặc mã đơn — cho phép ẩn danh.</summary>
    [AllowAnonymous]
    [HttpGet("lookup")]
    public async Task<ActionResult<WarrantyLookupResultDto>> Lookup([FromQuery] string? imei, [FromQuery] string? orderCode)
        => Ok(await _service.LookupAsync(imei, orderCode));

    /// <summary>Admin gán IMEI cho một dòng hàng của đơn và tạo phiếu bảo hành.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("orders/{orderId:int}/assign-imei")]
    public async Task<IActionResult> AssignImei(int orderId, AssignImeiDto dto)
    {
        await _service.AssignImeiAsync(orderId, dto);
        return Ok(new { message = "Đã gán IMEI và tạo phiếu bảo hành." });
    }

    /// <summary>Danh sách phiếu bảo hành của một đơn (admin).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("orders/{orderId:int}")]
    public async Task<ActionResult<IReadOnlyList<WarrantyDto>>> ByOrder(int orderId)
        => Ok(await _service.GetByOrderAsync(orderId));
}
