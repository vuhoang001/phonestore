using PhoneStore.Application.DTOs;

namespace PhoneStore.Application.Interfaces;

/// <summary>Tổng hợp báo cáo/analytics cho trang quản trị.</summary>
public interface IReportService
{
    Task<ReportSummaryDto> GetSummaryAsync(DateTime from, DateTime to);
    Task<RevenueReportDto> GetRevenueAsync(DateTime from, DateTime to, string groupBy);
    Task<List<TopProductReportDto>> GetTopProductsAsync(DateTime from, DateTime to, int limit);
    Task<List<CategoryRevenueDto>> GetRevenueByCategoryAsync(DateTime from, DateTime to);
    Task<List<PaymentMethodRevenueDto>> GetRevenueByPaymentAsync(DateTime from, DateTime to);
    Task<OrderStatsDto> GetOrderStatsAsync(DateTime from, DateTime to);
    Task<List<CountPointDto>> GetNewCustomersAsync(DateTime from, DateTime to, string groupBy);
    Task<InventoryReportDto> GetInventoryAsync();
}
