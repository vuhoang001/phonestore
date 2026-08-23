using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

/// <summary>Tổng hợp báo cáo. Doanh thu tính trên đơn đã hoàn tất (Completed).</summary>
public class ReportService : IReportService
{
    private const int LowStockThreshold = 10;
    private readonly IAppDbContext _db;
    public ReportService(IAppDbContext db) => _db = db;

    // Đơn tính doanh thu: đã hoàn tất, trong khoảng [from, to).
    private IQueryable<Domain.Entities.Order> CompletedOrders(DateTime from, DateTime to) =>
        _db.Orders.AsNoTracking().Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= from && o.CreatedAt < to);

    public async Task<ReportSummaryDto> GetSummaryAsync(DateTime from, DateTime to)
    {
        var orders = CompletedOrders(from, to);
        var revenue = await orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        var count = await orders.CountAsync();
        var discounts = await orders.SumAsync(o => (decimal?)o.DiscountAmount) ?? 0;
        var itemsSold = await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed && i.Order.CreatedAt >= from && i.Order.CreatedAt < to)
            .SumAsync(i => (int?)i.Quantity) ?? 0;
        var newCustomers = await _db.Users.AsNoTracking()
            .CountAsync(u => u.Role == UserRole.Customer && u.CreatedAt >= from && u.CreatedAt < to);

        return new ReportSummaryDto
        {
            Revenue = revenue,
            Orders = count,
            AvgOrderValue = count == 0 ? 0 : Math.Round(revenue / count, 0),
            ItemsSold = itemsSold,
            Discounts = discounts,
            NewCustomers = newCustomers
        };
    }

    public async Task<RevenueReportDto> GetRevenueAsync(DateTime from, DateTime to, string groupBy)
    {
        var orders = CompletedOrders(from, to);
        var series = new List<TimePointDto>();

        if (groupBy == "month")
        {
            var raw = await orders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
                .ToListAsync();
            foreach (var (y, m) in EachMonth(from, to))
            {
                var hit = raw.FirstOrDefault(r => r.Year == y && r.Month == m);
                series.Add(new TimePointDto { Label = $"{m:D2}/{y}", Revenue = hit?.Revenue ?? 0, Orders = hit?.Orders ?? 0 });
            }
        }
        else
        {
            var raw = await orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
                .ToListAsync();
            foreach (var day in EachDay(from, to))
            {
                var hit = raw.FirstOrDefault(r => r.Date == day);
                series.Add(new TimePointDto { Label = day.ToString("dd/MM"), Revenue = hit?.Revenue ?? 0, Orders = hit?.Orders ?? 0 });
            }
        }

        var total = series.Sum(s => s.Revenue);
        var totalOrders = series.Sum(s => s.Orders);
        return new RevenueReportDto
        {
            TotalRevenue = total,
            TotalOrders = totalOrders,
            AvgOrderValue = totalOrders == 0 ? 0 : Math.Round(total / totalOrders, 0),
            Series = series
        };
    }

    public async Task<List<TopProductReportDto>> GetTopProductsAsync(DateTime from, DateTime to, int limit)
    {
        return await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed && i.Order.CreatedAt >= from && i.Order.CreatedAt < to)
            .GroupBy(i => new { i.Variant.ProductId, i.ProductNameSnapshot })
            .Select(g => new TopProductReportDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductNameSnapshot,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.PriceSnapshot * i.Quantity)
            })
            .OrderByDescending(t => t.QuantitySold)
            .Take(limit <= 0 ? 10 : limit)
            .ToListAsync();
    }

    public async Task<List<CategoryRevenueDto>> GetRevenueByCategoryAsync(DateTime from, DateTime to)
    {
        return await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed && i.Order.CreatedAt >= from && i.Order.CreatedAt < to)
            .GroupBy(i => i.Variant.Product.Category.Name)
            .Select(g => new CategoryRevenueDto
            {
                Category = g.Key,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.PriceSnapshot * i.Quantity)
            })
            .OrderByDescending(c => c.Revenue)
            .ToListAsync();
    }

    public async Task<List<PaymentMethodRevenueDto>> GetRevenueByPaymentAsync(DateTime from, DateTime to)
    {
        var raw = await CompletedOrders(from, to)
            .Where(o => o.Payment != null)
            .GroupBy(o => o.Payment!.Method)
            .Select(g => new { Method = g.Key, Orders = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
            .ToListAsync();
        return raw.Select(r => new PaymentMethodRevenueDto
        {
            Method = r.Method.ToString(), Orders = r.Orders, Revenue = r.Revenue
        }).ToList();
    }

    public async Task<OrderStatsDto> GetOrderStatsAsync(DateTime from, DateTime to)
    {
        var all = _db.Orders.AsNoTracking().Where(o => o.CreatedAt >= from && o.CreatedAt < to);
        var byStatusRaw = await all.GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();

        var total = byStatusRaw.Sum(x => x.Count);
        var completed = byStatusRaw.FirstOrDefault(x => x.Status == OrderStatus.Completed)?.Count ?? 0;
        var cancelled = byStatusRaw.FirstOrDefault(x => x.Status == OrderStatus.Cancelled)?.Count ?? 0;
        var revenue = await all.Where(o => o.Status == OrderStatus.Completed).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        return new OrderStatsDto
        {
            Total = total,
            ByStatus = byStatusRaw.ToDictionary(x => x.Status.ToString(), x => x.Count),
            Completed = completed,
            Cancelled = cancelled,
            CompletionRate = total == 0 ? 0 : Math.Round(completed * 100m / total, 1),
            CancelRate = total == 0 ? 0 : Math.Round(cancelled * 100m / total, 1),
            AvgOrderValue = completed == 0 ? 0 : Math.Round(revenue / completed, 0)
        };
    }

    public async Task<List<CountPointDto>> GetNewCustomersAsync(DateTime from, DateTime to, string groupBy)
    {
        var users = _db.Users.AsNoTracking().Where(u => u.Role == UserRole.Customer && u.CreatedAt >= from && u.CreatedAt < to);
        var result = new List<CountPointDto>();

        if (groupBy == "month")
        {
            var raw = await users.GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();
            foreach (var (y, m) in EachMonth(from, to))
                result.Add(new CountPointDto { Label = $"{m:D2}/{y}", Count = raw.FirstOrDefault(r => r.Year == y && r.Month == m)?.Count ?? 0 });
        }
        else
        {
            var raw = await users.GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();
            foreach (var day in EachDay(from, to))
                result.Add(new CountPointDto { Label = day.ToString("dd/MM"), Count = raw.FirstOrDefault(r => r.Date == day)?.Count ?? 0 });
        }
        return result;
    }

    public async Task<InventoryReportDto> GetInventoryAsync()
    {
        var variants = _db.ProductVariants.AsNoTracking();
        var totalVariants = await variants.CountAsync();
        var totalStock = await variants.SumAsync(v => (int?)v.StockQuantity) ?? 0;
        var stockValue = await variants.SumAsync(v => (decimal?)(v.Price * v.StockQuantity)) ?? 0;
        var outOfStock = await variants.CountAsync(v => v.StockQuantity == 0);
        var lowStock = await variants.CountAsync(v => v.StockQuantity > 0 && v.StockQuantity < LowStockThreshold);
        var totalProducts = await _db.Products.AsNoTracking().CountAsync();

        var lowItems = await variants
            .Where(v => v.StockQuantity < LowStockThreshold)
            .OrderBy(v => v.StockQuantity)
            .Take(20)
            .Select(v => new LowStockItemDto
            {
                ProductId = v.ProductId,
                ProductName = v.Product.Name,
                Sku = v.Sku,
                Variant = (v.Color ?? "") + (v.Storage != null ? " " + v.Storage : ""),
                Stock = v.StockQuantity
            })
            .ToListAsync();

        return new InventoryReportDto
        {
            TotalProducts = totalProducts,
            TotalVariants = totalVariants,
            TotalStockUnits = totalStock,
            StockValue = stockValue,
            OutOfStockCount = outOfStock,
            LowStockCount = lowStock,
            LowStockItems = lowItems
        };
    }

    // ---------- helpers ----------
    private static IEnumerable<DateTime> EachDay(DateTime from, DateTime to)
    {
        for (var d = from.Date; d < to.Date; d = d.AddDays(1)) yield return d;
    }

    private static IEnumerable<(int Year, int Month)> EachMonth(DateTime from, DateTime to)
    {
        var cur = new DateTime(from.Year, from.Month, 1);
        var end = new DateTime(to.Year, to.Month, 1);
        while (cur <= end) { yield return (cur.Year, cur.Month); cur = cur.AddMonths(1); }
    }
}
