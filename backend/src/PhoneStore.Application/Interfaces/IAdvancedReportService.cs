using PhoneStore.Application.DTOs;

namespace PhoneStore.Application.Interfaces;

/// <summary>Báo cáo nâng cao v2 (tài chính, khách hàng, vận hành, hành vi).</summary>
public interface IAdvancedReportService
{
    // A · Tài chính
    Task<PromotionReportDto> GetPromotionAsync(DateTime from, DateTime to);
    Task<ReconciliationDto> GetReconciliationAsync(DateTime from, DateTime to);
    /// <summary>Drill-down đối soát: danh sách đơn tạo nên 1 con số (theo bucket thẻ, hoặc method+payStatus của dòng).</summary>
    Task<List<ReconOrderDto>> GetReconciliationOrdersAsync(DateTime from, DateTime to, string? bucket, string? method, string? payStatus);
    Task<ProfitReportDto> GetProfitAsync(DateTime from, DateTime to);
    /// <summary>Drill-down lợi nhuận: các đơn Completed (lọc theo danh mục nếu có) — nguồn của doanh thu/giá vốn/LN gộp.</summary>
    Task<List<ProfitOrderDto>> GetProfitOrdersAsync(DateTime from, DateTime to, string? category);
    /// <summary>Drill-down Tổng quan: các đơn hoàn tất — nguồn của Doanh thu/Đơn/Giá trị TB/SP đã bán/Giảm giá.</summary>
    Task<List<SummaryOrderDto>> GetSummaryOrdersAsync(DateTime from, DateTime to);
    /// <summary>Drill-down Tổng quan: các khách mới trong kỳ.</summary>
    Task<List<NewCustomerDto>> GetNewCustomerListAsync(DateTime from, DateTime to);
    /// <summary>Drill-down Tồn kho: các phân loại theo nhóm (low = sắp hết, out = hết hàng, all = tất cả).</summary>
    Task<List<InventoryItemDto>> GetInventoryItemsAsync(string? bucket);
    // B · Khách hàng
    /// <summary>Vòng đời khách tính đến mốc <paramref name="asOf"/> (thường là cuối kỳ lọc).</summary>
    Task<ChurnReportDto> GetChurnAsync(DateTime asOf);
    /// <summary>Drill-down vòng đời khách: danh sách khách trong 1 nhóm (active/atRisk/churned/never/all).</summary>
    Task<List<ChurnCustomerDto>> GetChurnCustomersAsync(DateTime asOf, string? bucket);
    Task<List<ProductPairDto>> GetMarketBasketAsync(DateTime from, DateTime to);
    Task<RfmReportDto> GetRfmAsync();
    // C · Vận hành
    Task<List<DemandItemDto>> GetDemandForecastAsync();
    Task<ProcessingTimeDto> GetProcessingTimeAsync(DateTime from, DateTime to);
    /// <summary>Drill-down thời gian xử lý: các đơn mẫu kèm giờ từng chặng.</summary>
    Task<List<ProcessingOrderDto>> GetProcessingOrdersAsync(DateTime from, DateTime to);
    Task<List<CancelReasonDto>> GetCancelReasonsAsync(DateTime from, DateTime to);
    // D · Hành vi & tìm kiếm
    Task<List<ViewToSaleDto>> GetViewToSaleAsync();
    Task<SearchReportDto> GetSearchReportAsync(DateTime from, DateTime to);
    Task<FunnelReportDto> GetFunnelAsync(DateTime from, DateTime to);
    // E · Báo cáo mở rộng
    Task<CohortReportDto> GetCohortAsync();
    Task<PeakTimeReportDto> GetPeakTimeAsync(DateTime from, DateTime to);
    Task<ReviewReportDto> GetReviewReportAsync(DateTime from, DateTime to);
    Task<FlashSaleReportDto> GetFlashSaleReportAsync();
}
