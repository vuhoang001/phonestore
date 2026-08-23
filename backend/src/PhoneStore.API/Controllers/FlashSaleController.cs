using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace PhoneStore.API.Controllers;

[Route("api/flash-sale")]
public class FlashSaleController : BaseApiController
{
    private readonly IFlashSaleService _service;
    public FlashSaleController(IFlashSaleService service) => _service = service;

    [HttpGet("active")]
    public async Task<ActionResult<FlashSaleDto?>> Active() => Ok(await _service.GetActiveAsync());

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<FlashSaleDto>>> All() => Ok(await _service.GetAllAsync());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<FlashSaleDto>> Create(CreateFlashSaleDto dto) => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FlashSaleDto>> Update(int id, CreateFlashSaleDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) { await _service.DeleteAsync(id); return NoContent(); }
}
