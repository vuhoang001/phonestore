using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PhoneStore.API.Controllers;

[Authorize]
[EnableRateLimiting("payment")]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _service;
    public PaymentsController(IPaymentService service) => _service = service;

    /// <summary>Tạo URL thanh toán VNPAY cho đơn hàng — client tự chuyển hướng sang URL trả về.</summary>
    [HttpPost("vnpay/{orderId:int}")]
    public async Task<ActionResult<CreatePaymentDto>> CreateVnPay(int orderId)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        return Ok(await _service.CreateVnPayUrlAsync(CurrentUserId, orderId, ip));
    }

    /// <summary>VNPAY gọi lại sau khi thanh toán. Verify chữ ký rồi redirect khách về frontend.</summary>
    [AllowAnonymous]
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> VnPayReturn()
    {
        var query = Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        var result = await _service.HandleVnPayReturnAsync(query);
        return Redirect(result.RedirectUrl);
    }

    /// <summary>
    /// IPN — VNPAY gọi server-to-server để chốt kết quả, độc lập với việc khách có quay lại hay không.
    /// Đây mới là nguồn tin cậy: dù khách rớt mạng lúc redirect, đơn vẫn được cập nhật.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> VnPayIpn()
    {
        var query = Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        var result = await _service.HandleVnPayReturnAsync(query);
        // VNPAY yêu cầu phản hồi JSON { RspCode, Message }.
        return Ok(new { RspCode = result.ResponseCode, Message = result.Message });
    }

    /// <summary>Trang giả lập gọi để chốt kết quả khi chưa có credential VNPAY thật.</summary>
    [HttpPost("vnpay/mock/{orderId:int}")]
    public async Task<ActionResult<PaymentResultDto>> CompleteMock(int orderId, [FromQuery] bool success = true)
        => Ok(await _service.CompleteMockAsync(CurrentUserId, orderId, success));
}
