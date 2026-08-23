using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

public class ReviewsController : BaseApiController
{
    private readonly IReviewService _service;
    public ReviewsController(IReviewService service) => _service = service;

    [HttpGet("~/api/products/{productId:int}/reviews")]
    public async Task<ActionResult<List<ReviewDto>>> ByProduct(int productId)
        => Ok(await _service.GetByProductAsync(productId));

    [Authorize]
    [HttpGet("can-review/{productId:int}")]
    public async Task<ActionResult<CanReviewDto>> CanReview(int productId)
        => Ok(await _service.CanReviewAsync(CurrentUserId, productId));

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewDto dto)
        => Ok(await _service.CreateAsync(CurrentUserId, dto));

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(CurrentUserId, CurrentRole, id);
        return NoContent();
    }
}
