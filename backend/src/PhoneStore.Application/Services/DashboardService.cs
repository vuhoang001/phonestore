using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IAppDbContext _db;
    public DashboardService(IAppDbContext db) => _db = db;

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        // Doanh thu tính trên đơn đã hoàn tất
        var completed = _db.Orders.AsNoTracking().Where(o => o.Status == OrderStatus.Completed);

        var totalRevenue = await completed.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        var totalOrders = await _db.Orders.CountAsync();
        var totalProducts = await _db.Products.CountAsync();
        var totalCustomers = await _db.Users.CountAsync(u => u.Role == UserRole.Customer);

        // Doanh thu 14 ngày gần nhất
        var since = DateTime.UtcNow.Date.AddDays(-13);
        var revenueRaw = await completed
            .Where(o => o.CreatedAt >= since)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        var revenueByDay = Enumerable.Range(0, 14)
            .Select(offset =>
            {
                var day = since.AddDays(offset);
                var match = revenueRaw.FirstOrDefault(r => r.Date == day);
                return new RevenuePointDto { Date = day.ToString("dd/MM"), Revenue = match?.Revenue ?? 0 };
            })
            .ToList();

        // Top 5 sản phẩm bán chạy
        var topProducts = await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed)
            .GroupBy(i => new { i.Variant.ProductId, i.ProductNameSnapshot })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductNameSnapshot,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.PriceSnapshot * i.Quantity)
            })
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToListAsync();

        var ordersByStatus = await _db.Orders.AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new DashboardStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            TotalProducts = totalProducts,
            TotalCustomers = totalCustomers,
            RevenueByDay = revenueByDay,
            TopProducts = topProducts,
            OrdersByStatus = ordersByStatus.ToDictionary(x => x.Status.ToString(), x => x.Count)
        };
    }
}
