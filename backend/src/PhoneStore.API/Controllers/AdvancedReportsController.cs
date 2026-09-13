using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

/// <summary>Báo cáo nâng cao v2. Cùng tiền tố /api/reports.</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Seller")]
public class AdvancedReportsController : ControllerBase
{
    private readonly IAdvancedReportService _svc;
    public AdvancedReportsController(IAdvancedReportService svc) => _svc = svc;

    private static (DateTime from, DateTime to) Range(DateTime? from, DateTime? to)
    {
        var end = (to ?? DateTime.UtcNow).Date.AddDays(1);
        var start = (from ?? DateTime.UtcNow.AddDays(-29)).Date;
        return (DateTime.SpecifyKind(start, DateTimeKind.Utc), DateTime.SpecifyKind(end, DateTimeKind.Utc));
    }

    // A · Tài chính
    [HttpGet("promotion")]
    public async Task<IActionResult> Promotion(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetPromotionAsync(f, t)); }

    [HttpGet("reconciliation")]
    public async Task<IActionResult> Reconciliation(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetReconciliationAsync(f, t)); }

    [HttpGet("reconciliation-orders")]
    public async Task<IActionResult> ReconciliationOrders(DateTime? from, DateTime? to, string? bucket, string? method, string? payStatus)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetReconciliationOrdersAsync(f, t, bucket, method, payStatus)); }

    [HttpGet("profit")]
    public async Task<IActionResult> Profit(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetProfitAsync(f, t)); }

    [HttpGet("profit-orders")]
    public async Task<IActionResult> ProfitOrders(DateTime? from, DateTime? to, string? category)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetProfitOrdersAsync(f, t, category)); }

    [HttpGet("summary-orders")]
    public async Task<IActionResult> SummaryOrders(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetSummaryOrdersAsync(f, t)); }

    [HttpGet("new-customer-list")]
    public async Task<IActionResult> NewCustomerList(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetNewCustomerListAsync(f, t)); }

    [HttpGet("inventory-items")]
    public async Task<IActionResult> InventoryItems(string? bucket) => Ok(await _svc.GetInventoryItemsAsync(bucket));

    // B · Khách hàng
    [HttpGet("churn")]
    public async Task<IActionResult> Churn(DateTime? from, DateTime? to)
    { var (_, t) = Range(from, to); return Ok(await _svc.GetChurnAsync(t)); }

    [HttpGet("churn-customers")]
    public async Task<IActionResult> ChurnCustomers(DateTime? from, DateTime? to, string? bucket)
    { var (_, t) = Range(from, to); return Ok(await _svc.GetChurnCustomersAsync(t, bucket)); }

    [HttpGet("market-basket")]
    public async Task<IActionResult> MarketBasket(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetMarketBasketAsync(f, t)); }

    [HttpGet("rfm")]
    public async Task<IActionResult> Rfm() => Ok(await _svc.GetRfmAsync());

    // C · Vận hành
    [HttpGet("demand-forecast")]
    public async Task<IActionResult> Demand() => Ok(await _svc.GetDemandForecastAsync());

    [HttpGet("processing-time")]
    public async Task<IActionResult> ProcessingTime(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetProcessingTimeAsync(f, t)); }

    [HttpGet("processing-orders")]
    public async Task<IActionResult> ProcessingOrders(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetProcessingOrdersAsync(f, t)); }

    [HttpGet("cancel-reasons")]
    public async Task<IActionResult> CancelReasons(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetCancelReasonsAsync(f, t)); }

    // D · Hành vi & tìm kiếm
    [HttpGet("view-to-sale")]
    public async Task<IActionResult> ViewToSale() => Ok(await _svc.GetViewToSaleAsync());

    [HttpGet("search")]
    public async Task<IActionResult> Search(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetSearchReportAsync(f, t)); }

    [HttpGet("funnel")]
    public async Task<IActionResult> Funnel(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetFunnelAsync(f, t)); }

    // E · Báo cáo mở rộng
    [HttpGet("cohort")]
    public async Task<IActionResult> Cohort() => Ok(await _svc.GetCohortAsync());

    [HttpGet("peak-time")]
    public async Task<IActionResult> PeakTime(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetPeakTimeAsync(f, t)); }

    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews(DateTime? from, DateTime? to)
    { var (f, t) = Range(from, to); return Ok(await _svc.GetReviewReportAsync(f, t)); }

    [HttpGet("flash-sale-perf")]
    public async Task<IActionResult> FlashSalePerf() => Ok(await _svc.GetFlashSaleReportAsync());
}
