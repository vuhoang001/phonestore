using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

public class ProductsController : BaseApiController
{
    private readonly IProductService _service;
    public ProductsController(IProductService service) => _service = service;

    // Bộ lọc bind từ query string, gồm cả brandId (lọc theo hãng điện thoại).
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> Search([FromQuery] ProductFilterDto filter)
        => Ok(await _service.SearchAsync(filter));

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ProductDetailDto>> BySlug(string slug)
        => Ok(await _service.GetBySlugAsync(slug));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> ById(int id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpGet("{id:int}/related")]
    public async Task<ActionResult<List<ProductListItemDto>>> Related(int id)
        => Ok(await _service.GetRelatedAsync(id));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDetailDto>> Create(CreateProductDto dto)
        => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> Update(int id, UpdateProductDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
