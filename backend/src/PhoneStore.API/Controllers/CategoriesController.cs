using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _service;
    public CategoriesController(ICategoryService service) => _service = service;

    [HttpGet("tree")]
    public async Task<ActionResult<List<CategoryDto>>> Tree() => Ok(await _service.GetTreeAsync());

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> All() => Ok(await _service.GetAllFlatAsync());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto dto)
        => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, CreateCategoryDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
