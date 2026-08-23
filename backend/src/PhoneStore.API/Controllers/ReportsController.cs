using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

/// <summary>Báo cáo/analytics cho quản trị. Khoảng ngày mặc định: 30 ngày gần nhất.</summary>
[Authorize(Roles = "Admin")]
public class ReportsController : BaseApiController
{
    private readonly IReportService _service;
    public ReportsController(IReportService service) => _service = service;

    // Chuẩn hóa khoảng ngày (UTC). 'to' được cộng 1 ngày để bao trọn ngày cuối.
    private static (DateTime from, DateTime to) Range(DateTime? from, DateTime? to)
    {
        var end = (to ?? DateTime.UtcNow).Date.AddDays(1);
        var start = (from ?? DateTime.UtcNow.AddDays(-29)).Date;
        return (DateTime.SpecifyKind(start, DateTimeKind.Utc), DateTime.SpecifyKind(end, DateTimeKind.Utc));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryDto>> Summary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (f, t) = Range(from, to);
        return Ok(await _service.GetSummaryAsync(f, t));
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string groupBy = "day")
    {
        var (f, t) = Range(from, to);
        return Ok(await _service.GetRevenueAsync(f, t, groupBy));
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<List<TopProductReportDto>>> TopProducts([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int limit = 10)
    {
        var (f, t) = Range(from, to);
        return Ok(await _service.GetTopProductsAsync(f, t, limit));
    }

    [HttpGet("revenue-by-category")]
    public async Task<ActionResult<List<CategoryRevenueDto>>> ByCategory([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (f, t) = Range(from, to);
        return Ok(await _service.GetRevenueByCategoryAsync(f, t));
    }

    [HttpGet("revenue-by-payment")]
    public async Task<ActionResult<List<PaymentMethodRevenueDto>>> ByPayment([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (f, t) = Range(from, to);
        return Ok(await _service.GetRevenueByPaymentAsync(f, t));
    }

    [HttpGet("order-stats")]
    public async Task<ActionResult<OrderStatsDto>> OrderStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (f, t) = Range(from, to);
        return Ok(await _service.GetOrderStatsAsync(f, t));
    }

    [HttpGet("new-customers")]
    public async Task<ActionResult<List<CountPointDto>>> NewCustomers([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string groupBy = "day")
    {
        var (f, t) = Range(from, to);
        return Ok(await _service.GetNewCustomersAsync(f, t, groupBy));
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryReportDto>> Inventory()
        => Ok(await _service.GetInventoryAsync());
}
