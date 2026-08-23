using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

/// <summary>Phương thức vận chuyển: đọc công khai cho checkout, CRUD cho admin.</summary>
public class ShippingController : BaseApiController
{
    private readonly IShippingService _service;
    public ShippingController(IShippingService service) => _service = service;

    [HttpGet("methods")]
    public async Task<ActionResult<List<ShippingMethodDto>>> Methods() => Ok(await _service.GetActiveAsync());

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<ShippingMethodDto>>> All() => Ok(await _service.GetAllAsync());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ShippingMethodDto>> Create(CreateShippingMethodDto dto)
        => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ShippingMethodDto>> Update(int id, CreateShippingMethodDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
