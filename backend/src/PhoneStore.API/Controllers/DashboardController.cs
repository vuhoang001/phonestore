using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

[Authorize(Roles = "Admin")]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _service;
    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> Stats() => Ok(await _service.GetStatsAsync());
}
