using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _service;
    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<NotificationListDto>> Mine()
        => Ok(await _service.GetMineAsync(CurrentUserId));

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> Read(int id)
    {
        await _service.MarkReadAsync(CurrentUserId, id);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> ReadAll()
    {
        await _service.MarkAllReadAsync(CurrentUserId);
        return NoContent();
    }
}
