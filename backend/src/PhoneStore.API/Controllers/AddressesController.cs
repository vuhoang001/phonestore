using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

[Authorize]
public class AddressesController : BaseApiController
{
    private readonly IAddressService _service;
    public AddressesController(IAddressService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<AddressDto>>> Mine() => Ok(await _service.GetMineAsync(CurrentUserId));

    [HttpPost]
    public async Task<ActionResult<AddressDto>> Create(CreateAddressDto dto)
        => Ok(await _service.CreateAsync(CurrentUserId, dto));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AddressDto>> Update(int id, CreateAddressDto dto)
        => Ok(await _service.UpdateAsync(CurrentUserId, id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(CurrentUserId, id);
        return NoContent();
    }
}
