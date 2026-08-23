using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

/// <summary>Thu cũ đổi mới: khách tạo/xem yêu cầu của mình; admin xem tất cả/báo giá/đổi trạng thái.</summary>
[Authorize]
[Route("api/trade-in")]
public class TradeInController : BaseApiController
{
    private readonly ITradeInService _service;
    public TradeInController(ITradeInService service) => _service = service;

    /// <summary>Khách tạo yêu cầu thu cũ đổi mới.</summary>
    [HttpPost]
    public async Task<ActionResult<TradeInDto>> Create(CreateTradeInDto dto)
        => Ok(await _service.CreateAsync(CurrentUserId, dto));

    /// <summary>Danh sách yêu cầu của chính khách hàng.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<TradeInDto>>> Mine()
        => Ok(await _service.GetMineAsync(CurrentUserId));

    /// <summary>Admin xem tất cả yêu cầu.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TradeInDto>>> All()
        => Ok(await _service.GetAllAsync());

    /// <summary>Admin báo giá thu cho một yêu cầu.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/quote")]
    public async Task<ActionResult<TradeInDto>> Quote(int id, QuoteTradeInDto dto)
        => Ok(await _service.QuoteAsync(id, dto));

    /// <summary>Admin đổi trạng thái yêu cầu (duyệt/từ chối/hoàn tất...).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<TradeInDto>> UpdateStatus(int id, [FromQuery] string status)
        => Ok(await _service.UpdateStatusAsync(id, status));
}
