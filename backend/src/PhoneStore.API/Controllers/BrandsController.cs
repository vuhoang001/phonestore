using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

/// <summary>Thương hiệu điện thoại (Apple, Samsung...): đọc công khai, CRUD cho admin.</summary>
public class BrandsController : BaseApiController
{
    private readonly IBrandService _service;
    public BrandsController(IBrandService service) => _service = service;

    /// <summary>Danh sách hãng để hiển thị/lọc phía client.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> All() => Ok(await _service.GetAllAsync());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<BrandDto>> Create(BrandDto dto) => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BrandDto>> Update(int id, BrandDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
