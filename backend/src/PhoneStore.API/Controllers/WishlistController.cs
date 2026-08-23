using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

[Authorize]
public class WishlistController : BaseApiController
{
    private readonly IWishlistService _service;
    public WishlistController(IWishlistService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<WishlistItemDto>>> Mine()
        => Ok(await _service.GetMineAsync(CurrentUserId));

    [HttpPost("toggle/{productId:int}")]
    public async Task<IActionResult> Toggle(int productId)
    {
        await _service.ToggleAsync(CurrentUserId, productId);
        return Ok(new { message = "OK" });
    }
}
