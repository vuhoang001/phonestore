using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

public class CouponsController : BaseApiController
{
    private readonly ICouponService _service;
    public CouponsController(ICouponService service) => _service = service;

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<CouponDto>>> All() => Ok(await _service.GetAllAsync());

    /// <summary>Kho voucher: các mã đang có hiệu lực để khách lưu/sử dụng.</summary>
    [HttpGet("available")]
    public async Task<ActionResult<List<CouponDto>>> Available() => Ok(await _service.GetAvailableAsync());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CouponDto>> Create(CreateCouponDto dto) => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Kiểm tra mã giảm giá hợp lệ với giá trị đơn hàng.</summary>
    [Authorize]
    [HttpGet("validate")]
    public async Task<ActionResult<CouponDto>> Validate([FromQuery] string code, [FromQuery] decimal orderAmount)
        => Ok(await _service.ValidateAsync(code, orderAmount));
}
