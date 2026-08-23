using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

[Authorize]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _service;
    public OrdersController(IOrderService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderDto dto)
        => Ok(await _service.CreateAsync(CurrentUserId, dto));

    [HttpGet("my")]
    public async Task<ActionResult<PagedResult<OrderDto>>> My([FromQuery] PaginationQuery query, [FromQuery] string? status)
        => Ok(await _service.GetMyOrdersAsync(CurrentUserId, query, status));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> ById(int id)
        => Ok(await _service.GetByIdAsync(CurrentUserId, CurrentRole, id));

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(int id, [FromQuery] string? reason)
        => Ok(await _service.CancelByCustomerAsync(CurrentUserId, id, reason));

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> All([FromQuery] PaginationQuery query, [FromQuery] string? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? keyword)
        => Ok(await _service.GetAllAsync(query, status, from, to, keyword));

    // Chuyển trạng thái đơn. Khi sang Shipping, dto.ImeiAssignments mang IMEI từng dòng máy
    // để service tạo phiếu bảo hành (đặc thù điện thoại).
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, UpdateOrderStatusDto dto)
        => Ok(await _service.UpdateStatusAsync(id, dto));
}
