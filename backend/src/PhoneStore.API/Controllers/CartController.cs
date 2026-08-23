using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

/// <summary>Giỏ hàng: hỗ trợ cả người dùng đã đăng nhập lẫn khách vãng lai (qua header X-Cart-Token).</summary>
[AllowAnonymous]
public class CartController : BaseApiController
{
    private readonly ICartService _service;
    public CartController(ICartService service) => _service = service;

    /// <summary>Xác định chủ giỏ: user đã đăng nhập, hoặc khách qua token gửi kèm header.</summary>
    private CartOwner Owner()
    {
        if (User.Identity?.IsAuthenticated == true)
            return CartOwner.ForUser(CurrentUserId);
        var token = Request.Headers["X-Cart-Token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
            throw new AppException("Thiếu định danh giỏ hàng (X-Cart-Token).");
        return CartOwner.ForGuest(token);
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get() => Ok(await _service.GetAsync(Owner()));

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> Add(AddToCartDto dto)
        => Ok(await _service.AddAsync(Owner(), dto));

    [HttpPut("items/{itemId:int}")]
    public async Task<ActionResult<CartDto>> Update(int itemId, UpdateCartItemDto dto)
        => Ok(await _service.UpdateItemAsync(Owner(), itemId, dto));

    [HttpDelete("items/{itemId:int}")]
    public async Task<ActionResult<CartDto>> Remove(int itemId)
        => Ok(await _service.RemoveItemAsync(Owner(), itemId));

    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        await _service.ClearAsync(Owner());
        return NoContent();
    }

    /// <summary>Gộp giỏ khách vào giỏ user sau khi đăng nhập.</summary>
    [Authorize]
    [HttpPost("merge")]
    public async Task<ActionResult<CartDto>> Merge([FromQuery] string guestToken)
        => Ok(await _service.MergeAsync(CurrentUserId, guestToken));
}
