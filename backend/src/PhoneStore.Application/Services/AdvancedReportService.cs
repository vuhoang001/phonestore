using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

/// <summary>Báo cáo nâng cao. Dữ liệu nhỏ (demo) nên phần phức tạp tính trong bộ nhớ.</summary>
public class AdvancedReportService : IAdvancedReportService
{
    private const int LowStockThreshold = 10;
    private readonly IAppDbContext _db;
    public AdvancedReportService(IAppDbContext db) => _db = db;

    private IQueryable<Domain.Entities.Order> Completed(DateTime from, DateTime to) =>
        _db.Orders.AsNoTracking().Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= from && o.CreatedAt < to);

    // ============ A · TÀI CHÍNH ============
    public async Task<PromotionReportDto> GetPromotionAsync(DateTime from, DateTime to)
    {
        var orders = await Completed(from, to)
            .Select(o => new
            {
                o.TotalAmount,
                o.DiscountAmount,
                Codes = o.OrderCoupons.Select(c => c.Coupon.Code).ToList()
            }).ToListAsync();

        var withCoupon = orders.Where(o => o.Codes.Count > 0).ToList();
        var without = orders.Where(o => o.Codes.Count == 0).ToList();

        var couponPerf = orders
            .SelectMany(o => o.Codes.Select(code => new { code, o.DiscountAmount, o.TotalAmount }))
            .GroupBy(x => x.code)
            .Select(g => new CouponPerfDto
            {
                Code = g.Key,
                TimesUsed = g.Count(),
                TotalDiscount = g.Sum(x => x.DiscountAmount),
                RevenueGenerated = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(c => c.RevenueGenerated)
            .ToList();

        return new PromotionReportDto
        {
            TotalDiscount = orders.Sum(o => o.DiscountAmount),
            OrdersWithCoupon = withCoupon.Count,
            RevenueWithCoupon = withCoupon.Sum(o => o.TotalAmount),
            AovWithCoupon = withCoupon.Count == 0 ? 0 : Math.Round(withCoupon.Sum(o => o.TotalAmount) / withCoupon.Count, 0),
            AovWithoutCoupon = without.Count == 0 ? 0 : Math.Round(without.Sum(o => o.TotalAmount) / without.Count, 0),
            Coupons = couponPerf
        };
    }

    public async Task<ReconciliationDto> GetReconciliationAsync(DateTime from, DateTime to)
    {
        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to && o.Payment != null)
            .Select(o => new { o.TotalAmount, o.Status, Method = o.Payment!.Method, PayStatus = o.Payment.Status })
            .ToListAsync();

        bool IsCod(PaymentMethod m) => m == PaymentMethod.Cod;
        var codPendingOrders = orders.Where(o => IsCod(o.Method) && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Shipping)).ToList();

        var rows = orders
            .GroupBy(o => new { o.Method, o.PayStatus })
            .Select(g => new PaymentRowDto
            {
                Method = g.Key.Method.ToString(),
                Status = g.Key.PayStatus.ToString(),
                Orders = g.Count(),
                Amount = g.Sum(x => x.TotalAmount)
            })
            .OrderBy(r => r.Method).ThenBy(r => r.Status)
            .ToList();

        return new ReconciliationDto
        {
            CodPending = codPendingOrders.Sum(o => o.TotalAmount),
            CodPendingOrders = codPendingOrders.Count,
            CodCollected = orders.Where(o => IsCod(o.Method) && o.PayStatus == PaymentStatus.Paid).Sum(o => o.TotalAmount),
            OnlinePaid = orders.Where(o => !IsCod(o.Method) && o.PayStatus == PaymentStatus.Paid).Sum(o => o.TotalAmount),
            OnlinePending = orders.Where(o => !IsCod(o.Method) && o.PayStatus == PaymentStatus.Pending && o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
            Rows = rows
        };
    }

    public async Task<List<ReconOrderDto>> GetReconciliationOrdersAsync(DateTime from, DateTime to, string? bucket, string? method, string? payStatus)
    {
        var q = _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to && o.Payment != null);

        if (!string.IsNullOrEmpty(bucket))
        {
            // 4 thẻ tổng — đúng công thức như GetReconciliationAsync.
            q = bucket switch
            {
                "codPending" => q.Where(o => o.Payment!.Method == PaymentMethod.Cod && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Shipping)),
                "codCollected" => q.Where(o => o.Payment!.Method == PaymentMethod.Cod && o.Payment.Status == PaymentStatus.Paid),
                "onlinePaid" => q.Where(o => o.Payment!.Method != PaymentMethod.Cod && o.Payment.Status == PaymentStatus.Paid),
                "onlinePending" => q.Where(o => o.Payment!.Method != PaymentMethod.Cod && o.Payment.Status == PaymentStatus.Pending && o.Status != OrderStatus.Cancelled),
                _ => q
            };
        }
        else
        {
            // Drill từ 1 dòng bảng: theo phương thức + trạng thái thanh toán.
            if (Enum.TryParse<PaymentMethod>(method, true, out var m)) q = q.Where(o => o.Payment!.Method == m);
            if (Enum.TryParse<PaymentStatus>(payStatus, true, out var ps)) q = q.Where(o => o.Payment!.Status == ps);
        }

        return await q.OrderByDescending(o => o.CreatedAt).Take(300)
            .Select(o => new ReconOrderDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                CreatedAt = o.CreatedAt,
                OrderStatus = o.Status.ToString(),
                Method = o.Payment!.Method.ToString(),
                PayStatus = o.Payment.Status.ToString(),
                Amount = o.TotalAmount
            })
            .ToListAsync();
    }

    public async Task<ProfitReportDto> GetProfitAsync(DateTime from, DateTime to)
    {
        var lines = await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed && i.Order.CreatedAt >= from && i.Order.CreatedAt < to)
            .Select(i => new
            {
                Category = i.Variant.Product.Category.Name,
                Revenue = i.PriceSnapshot * i.Quantity,
                Cost = i.Variant.Cost * i.Quantity
            }).ToListAsync();

        var byCat = lines.GroupBy(l => l.Category).Select(g =>
        {
            var rev = g.Sum(x => x.Revenue);
            var cost = g.Sum(x => x.Cost);
            return new ProfitCategoryDto
            {
                Category = g.Key, Revenue = rev, Cogs = cost, Profit = rev - cost,
                MarginPct = rev == 0 ? 0 : Math.Round((rev - cost) * 100 / rev, 1)
            };
        }).OrderByDescending(c => c.Profit).ToList();

        var totalRev = lines.Sum(l => l.Revenue);
        var totalCost = lines.Sum(l => l.Cost);
        var discounts = await Completed(from, to).SumAsync(o => (decimal?)o.DiscountAmount) ?? 0;
        var shipping = await Completed(from, to).SumAsync(o => (decimal?)o.ShippingFee) ?? 0;

        return new ProfitReportDto
        {
            Revenue = totalRev, Cogs = totalCost, GrossProfit = totalRev - totalCost,
            GrossMarginPct = totalRev == 0 ? 0 : Math.Round((totalRev - totalCost) * 100 / totalRev, 1),
            Discounts = discounts, ShippingCharged = shipping, ByCategory = byCat
        };
    }

    public async Task<List<ProfitOrderDto>> GetProfitOrdersAsync(DateTime from, DateTime to, string? category)
    {
        var q = _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed && i.Order.CreatedAt >= from && i.Order.CreatedAt < to);
        if (!string.IsNullOrEmpty(category))
            q = q.Where(i => i.Variant.Product.Category.Name == category);

        var list = await q
            .GroupBy(i => new { i.OrderId, i.Order.OrderCode, i.Order.CreatedAt, i.Order.DiscountAmount, i.Order.ShippingFee })
            .Select(g => new ProfitOrderDto
            {
                Id = g.Key.OrderId,
                OrderCode = g.Key.OrderCode,
                CreatedAt = g.Key.CreatedAt,
                Revenue = g.Sum(x => x.PriceSnapshot * x.Quantity),
                Cogs = g.Sum(x => x.Variant.Cost * x.Quantity),
                Profit = g.Sum(x => x.PriceSnapshot * x.Quantity) - g.Sum(x => x.Variant.Cost * x.Quantity),
                Discount = g.Key.DiscountAmount,
                Shipping = g.Key.ShippingFee
            })
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .ToListAsync();

        // Lọc theo danh mục thì giảm giá/phí ship (cấp đơn) không thuộc riêng danh mục → về 0.
        if (!string.IsNullOrEmpty(category))
            list = list.Select(x => x with { Discount = 0, Shipping = 0 }).ToList();
        return list;
    }

    // Drill-down Tổng quan: các đơn hoàn tất (giống công thức GetSummaryAsync).
    // Σ Total = Doanh thu · Count = Đơn · Σ ItemCount = SP đã bán · Σ Discount = Đã giảm giá.
    public async Task<List<SummaryOrderDto>> GetSummaryOrdersAsync(DateTime from, DateTime to)
    {
        return await Completed(from, to)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new SummaryOrderDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                CreatedAt = o.CreatedAt,
                Status = o.Status.ToString(),
                ItemCount = o.Items.Sum(i => (int?)i.Quantity) ?? 0,
                Discount = o.DiscountAmount,
                Total = o.TotalAmount
            })
            .ToListAsync();
    }

    // Drill-down Tổng quan: khách đăng ký trong kỳ (khớp "Khách mới").
    public async Task<List<NewCustomerDto>> GetNewCustomerListAsync(DateTime from, DateTime to)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Customer && u.CreatedAt >= from && u.CreatedAt < to)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new NewCustomerDto { UserId = u.Id, Name = u.FullName, Email = u.Email, CreatedAt = u.CreatedAt })
            .ToListAsync();
    }

    // Drill-down Tồn kho: phân loại theo nhóm. Σ Value = Giá trị tồn · Σ Stock = Đơn vị tồn.
    public async Task<List<InventoryItemDto>> GetInventoryItemsAsync(string? bucket)
    {
        // IgnoreQueryFilters: khớp đúng KPI tồn kho (đếm mọi variant, kể cả của SP đã ẩn/xoá mềm).
        var q = _db.ProductVariants.AsNoTracking().IgnoreQueryFilters();
        q = bucket switch
        {
            "low" => q.Where(v => v.StockQuantity > 0 && v.StockQuantity < LowStockThreshold),
            "out" => q.Where(v => v.StockQuantity == 0),
            "lowout" => q.Where(v => v.StockQuantity < LowStockThreshold),
            _ => q // all
        };
        return await q.OrderBy(v => v.StockQuantity)
            .Select(v => new InventoryItemDto
            {
                ProductId = v.ProductId,
                ProductName = v.Product.Name,
                Sku = v.Sku,
                Variant = (v.Color ?? "") + (v.Storage != null ? " · " + v.Storage : ""),
                Stock = v.StockQuantity,
                Price = v.Price,
                Value = v.Price * v.StockQuantity
            })
            .ToListAsync();
    }

    // ============ B · KHÁCH HÀNG ============
    private record CustomerAgg(int UserId, string Name, string Email, DateTime? Last, int Freq, decimal Spent);

    private async Task<List<CustomerAgg>> LoadCustomerAggAsync(DateTime? upTo = null)
    {
        var customers = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Customer)
            .Select(u => new { u.Id, u.FullName, u.Email }).ToListAsync();

        // Gộp đơn theo khách: lần đặt gần nhất + số đơn hoàn tất + tổng chi.
        // upTo: chỉ tính đơn trước mốc này (để phân tích vòng đời "tính đến" 1 thời điểm).
        var ordersQ = _db.Orders.AsNoTracking().AsQueryable();
        if (upTo is not null) ordersQ = ordersQ.Where(o => o.CreatedAt < upTo.Value);
        var agg = await ordersQ
            .GroupBy(o => o.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Last = (DateTime?)g.Max(o => o.CreatedAt),
                Freq = g.Count(o => o.Status == OrderStatus.Completed),
                Spent = g.Where(o => o.Status == OrderStatus.Completed).Sum(o => (decimal?)o.TotalAmount) ?? 0
            }).ToListAsync();

        var map = agg.ToDictionary(a => a.UserId);
        return customers.Select(c =>
        {
            map.TryGetValue(c.Id, out var a);
            return new CustomerAgg(c.Id, c.FullName, c.Email, a?.Last, a?.Freq ?? 0, a?.Spent ?? 0);
        }).ToList();
    }

    public async Task<ChurnReportDto> GetChurnAsync(DateTime asOf)
    {
        var custs = await LoadCustomerAggAsync(asOf);
        int Days(DateTime? d) => d is null ? int.MaxValue : (int)(asOf - d.Value).TotalDays;

        var ordered = custs.Where(c => c.Last != null).ToList();
        var churnedList = ordered.Where(c => Days(c.Last) > 90)
            .OrderByDescending(c => Days(c.Last))
            .Take(20)
            .Select(c => new ChurnCustomerDto
            {
                UserId = c.UserId, Name = c.Name, Email = c.Email,
                LastOrder = c.Last, DaysSince = Days(c.Last), TotalSpent = c.Spent
            }).ToList();

        return new ChurnReportDto
        {
            TotalCustomers = custs.Count,
            Active = ordered.Count(c => Days(c.Last) <= 30),
            AtRisk = ordered.Count(c => Days(c.Last) is > 30 and <= 90),
            Churned = ordered.Count(c => Days(c.Last) > 90),
            NeverOrdered = custs.Count(c => c.Last == null),
            TopChurned = churnedList
        };
    }

    // Drill-down vòng đời: khách trong 1 nhóm (số lượng khớp thẻ KPI của GetChurnAsync).
    public async Task<List<ChurnCustomerDto>> GetChurnCustomersAsync(DateTime asOf, string? bucket)
    {
        var custs = await LoadCustomerAggAsync(asOf);
        int Days(DateTime? d) => d is null ? int.MaxValue : (int)(asOf - d.Value).TotalDays;

        IEnumerable<CustomerAgg> sel = bucket switch
        {
            "active" => custs.Where(c => c.Last != null && Days(c.Last) <= 30),
            "atRisk" => custs.Where(c => c.Last != null && Days(c.Last) is > 30 and <= 90),
            "churned" => custs.Where(c => c.Last != null && Days(c.Last) > 90),
            "never" => custs.Where(c => c.Last == null),
            _ => custs // all = Tổng khách
        };

        return sel
            .OrderBy(c => c.Last == null) // đã từng mua lên trước
            .ThenByDescending(c => Days(c.Last))
            .Select(c => new ChurnCustomerDto
            {
                UserId = c.UserId, Name = c.Name, Email = c.Email,
                LastOrder = c.Last, DaysSince = c.Last == null ? 0 : Days(c.Last), TotalSpent = c.Spent
            }).ToList();
    }

    public async Task<List<ProductPairDto>> GetMarketBasketAsync(DateTime from, DateTime to)
    {
        var lines = await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed && i.Order.CreatedAt >= from && i.Order.CreatedAt < to)
            .Select(i => new { i.OrderId, i.Variant.ProductId, i.ProductNameSnapshot })
            .ToListAsync();

        var pairs = new Dictionary<(int, int), (int count, string a, string b)>();
        foreach (var grp in lines.GroupBy(l => l.OrderId))
        {
            var prods = grp.GroupBy(x => x.ProductId).Select(g => (id: g.Key, name: g.First().ProductNameSnapshot))
                .OrderBy(p => p.id).ToList();
            for (var i = 0; i < prods.Count; i++)
                for (var j = i + 1; j < prods.Count; j++)
                {
                    var key = (prods[i].id, prods[j].id);
                    if (pairs.TryGetValue(key, out var v)) pairs[key] = (v.count + 1, v.a, v.b);
                    else pairs[key] = (1, prods[i].name, prods[j].name);
                }
        }

        return pairs.Values.Where(v => v.count >= 1)
            .OrderByDescending(v => v.count)
            .Take(15)
            .Select(v => new ProductPairDto { ProductA = v.a, ProductB = v.b, Count = v.count })
            .ToList();
    }

    public async Task<RfmReportDto> GetRfmAsync()
    {
        var now = DateTime.UtcNow;
        var custs = (await LoadCustomerAggAsync()).Where(c => c.Freq > 0).ToList();
        int Days(DateTime? d) => d is null ? 9999 : (int)(now - d!.Value).TotalDays;

        string Segment(CustomerAgg c)
        {
            var r = Days(c.Last);
            if (r <= 30 && c.Freq >= 3) return "Khách VIP";
            if (c.Freq >= 2 && r <= 60) return "Trung thành";
            if (c.Freq == 1 && r <= 30) return "Khách mới";
            if (r is > 60 and <= 90) return "Có nguy cơ rời";
            if (r > 90) return "Đã rời bỏ";
            return "Bình thường";
        }

        var segs = custs.GroupBy(Segment)
            .Select(g => new RfmSegmentDto { Segment = g.Key, Customers = g.Count(), Revenue = g.Sum(c => c.Spent) })
            .OrderByDescending(s => s.Revenue)
            .ToList();
        return new RfmReportDto { Segments = segs };
    }

    // ============ C · VẬN HÀNH ============
    public async Task<List<DemandItemDto>> GetDemandForecastAsync()
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var sold = await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed && i.Order.CreatedAt >= since)
            .GroupBy(i => i.VariantId)
            .Select(g => new { VariantId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToListAsync();
        var soldMap = sold.ToDictionary(s => s.VariantId, s => s.Qty);

        var variants = await _db.ProductVariants.AsNoTracking()
            .Select(v => new { v.Id, v.Sku, v.StockQuantity, ProductId = v.ProductId, ProductName = v.Product.Name, v.Color, v.Storage })
            .ToListAsync();

        var items = variants.Select(v =>
        {
            var qty = soldMap.TryGetValue(v.Id, out var q) ? q : 0;
            var avgDaily = Math.Round(qty / 30.0, 2);
            var daysLeft = avgDaily > 0 ? Math.Round(v.StockQuantity / avgDaily, 1) : 9999;
            var reorder = (int)Math.Max(0, Math.Ceiling(avgDaily * 30 - v.StockQuantity));
            return new DemandItemDto
            {
                ProductId = v.ProductId,
                ProductName = v.ProductName + (string.IsNullOrEmpty(v.Storage) ? "" : $" ({v.Color} {v.Storage})"),
                Sku = v.Sku, Stock = v.StockQuantity, AvgDailySold = avgDaily, DaysLeft = daysLeft, SuggestedReorder = reorder
            };
        })
        .Where(i => i.AvgDailySold > 0) // chỉ SP có bán
        .OrderBy(i => i.DaysLeft)
        .Take(30)
        .ToList();
        return items;
    }

    public async Task<ProcessingTimeDto> GetProcessingTimeAsync(DateTime from, DateTime to)
    {
        var orders = await Completed(from, to)
            .Select(o => o.StatusHistory.Select(h => new { h.Status, h.CreatedAt }).ToList())
            .ToListAsync();

        double confirm = 0, ship = 0, complete = 0, totalH = 0; int n = 0;
        foreach (var hist in orders)
        {
            DateTime? T(OrderStatus s) => hist.Where(h => h.Status == s).Select(h => (DateTime?)h.CreatedAt).FirstOrDefault();
            var p = T(OrderStatus.Pending); var c = T(OrderStatus.Confirmed);
            var s2 = T(OrderStatus.Shipping); var d = T(OrderStatus.Completed);
            if (p is null || d is null) continue;
            n++;
            if (c is not null) confirm += (c.Value - p.Value).TotalHours;
            if (s2 is not null && c is not null) ship += (s2.Value - c.Value).TotalHours;
            if (d is not null && s2 is not null) complete += (d.Value - s2.Value).TotalHours;
            totalH += (d.Value - p.Value).TotalHours;
        }

        double Avg(double sum) => n == 0 ? 0 : Math.Round(sum / n, 1);
        return new ProcessingTimeDto
        {
            AvgConfirmHours = Avg(confirm), AvgShipHours = Avg(ship),
            AvgCompleteHours = Avg(complete), AvgTotalHours = Avg(totalH), SampleSize = n
        };
    }

    // Drill-down: từng đơn mẫu kèm giờ mỗi chặng (trung bình các cột = KPI ở trên).
    public async Task<List<ProcessingOrderDto>> GetProcessingOrdersAsync(DateTime from, DateTime to)
    {
        var orders = await Completed(from, to)
            .Select(o => new { o.Id, o.OrderCode, o.CreatedAt, Hist = o.StatusHistory.Select(h => new { h.Status, h.CreatedAt }).ToList() })
            .ToListAsync();

        var res = new List<ProcessingOrderDto>();
        foreach (var o in orders)
        {
            DateTime? T(OrderStatus s) => o.Hist.Where(h => h.Status == s).Select(h => (DateTime?)h.CreatedAt).FirstOrDefault();
            var p = T(OrderStatus.Pending); var c = T(OrderStatus.Confirmed);
            var s2 = T(OrderStatus.Shipping); var d = T(OrderStatus.Completed);
            if (p is null || d is null) continue;
            res.Add(new ProcessingOrderDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                CreatedAt = o.CreatedAt,
                ConfirmHours = c is not null ? Math.Round((c.Value - p.Value).TotalHours, 1) : 0,
                ShipHours = s2 is not null && c is not null ? Math.Round((s2.Value - c.Value).TotalHours, 1) : 0,
                CompleteHours = d is not null && s2 is not null ? Math.Round((d.Value - s2.Value).TotalHours, 1) : 0,
                TotalHours = Math.Round((d.Value - p.Value).TotalHours, 1)
            });
        }
        return res.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public async Task<List<CancelReasonDto>> GetCancelReasonsAsync(DateTime from, DateTime to)
    {
        var raw = await _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Cancelled && o.CreatedAt >= from && o.CreatedAt < to)
            .Select(o => new { o.CancelReason, o.TotalAmount })
            .ToListAsync();

        return raw.GroupBy(o => string.IsNullOrWhiteSpace(o.CancelReason) ? "Không rõ" : o.CancelReason!)
            .Select(g => new CancelReasonDto { Reason = g.Key, Count = g.Count(), LostRevenue = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(c => c.Count)
            .ToList();
    }

    // ============ D · HÀNH VI & TÌM KIẾM ============
    public async Task<List<ViewToSaleDto>> GetViewToSaleAsync()
    {
        var products = await _db.Products.AsNoTracking()
            .Where(p => p.ViewCount > 0)
            .Select(p => new { p.Id, p.Name, p.ViewCount, p.SoldCount })
            .OrderByDescending(p => p.ViewCount)
            .Take(30)
            .ToListAsync();

        return products.Select(p => new ViewToSaleDto
        {
            ProductId = p.Id, ProductName = p.Name, Views = p.ViewCount, Sold = p.SoldCount,
            ConversionRate = p.ViewCount == 0 ? 0 : Math.Round(p.SoldCount * 100.0 / p.ViewCount, 2)
        }).ToList();
    }

    public async Task<SearchReportDto> GetSearchReportAsync(DateTime from, DateTime to)
    {
        var logs = await _db.SearchLogs.AsNoTracking()
            .Where(s => s.CreatedAt >= from && s.CreatedAt < to)
            .Select(s => new { s.Keyword, s.ResultCount }).ToListAsync();

        var top = logs.GroupBy(l => l.Keyword)
            .Select(g => new KeywordStatDto { Keyword = g.Key, Count = g.Count(), AvgResults = (int)g.Average(x => x.ResultCount) })
            .OrderByDescending(k => k.Count).Take(15).ToList();

        var noResult = logs.Where(l => l.ResultCount == 0).GroupBy(l => l.Keyword)
            .Select(g => new KeywordStatDto { Keyword = g.Key, Count = g.Count(), AvgResults = 0 })
            .OrderByDescending(k => k.Count).Take(15).ToList();

        return new SearchReportDto { TotalSearches = logs.Count, TopKeywords = top, NoResultKeywords = noResult };
    }

    public async Task<FunnelReportDto> GetFunnelAsync(DateTime from, DateTime to)
    {
        var views = await _db.Products.AsNoTracking().SumAsync(p => (long?)p.ViewCount) ?? 0;
        var cartItems = await _db.CartItems.AsNoTracking().CountAsync();
        var orders = await _db.Orders.AsNoTracking().CountAsync(o => o.CreatedAt >= from && o.CreatedAt < to);
        var completed = await _db.Orders.AsNoTracking().CountAsync(o => o.Status == OrderStatus.Completed && o.CreatedAt >= from && o.CreatedAt < to);

        return new FunnelReportDto
        {
            ProductViews = (int)Math.Min(views, int.MaxValue),
            CartItems = cartItems,
            Orders = orders,
            CompletedOrders = completed,
            ViewToOrderRate = views == 0 ? 0 : Math.Round(orders * 100.0 / views, 3),
            OrderCompletionRate = orders == 0 ? 0 : Math.Round(completed * 100.0 / orders, 1)
        };
    }

    // ============ E · BÁO CÁO MỞ RỘNG ============

    // Cohort giữ chân: nhóm khách theo THÁNG mua đầu → % quay lại ở các tháng sau.
    public async Task<CohortReportDto> GetCohortAsync()
    {
        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed)
            .Select(o => new { o.UserId, o.CreatedAt }).ToListAsync();
        if (orders.Count == 0) return new CohortReportDto();

        const int maxOffset = 5; // 6 cột: Tháng 0..5
        var perUser = orders.GroupBy(o => o.UserId).Select(g => new
        {
            First = new DateTime(g.Min(x => x.CreatedAt).Year, g.Min(x => x.CreatedAt).Month, 1),
            Months = g.Select(x => new DateTime(x.CreatedAt.Year, x.CreatedAt.Month, 1)).Distinct().ToHashSet()
        }).ToList();

        var cohorts = perUser.Select(u => u.First).Distinct().OrderBy(d => d).ToList();
        if (cohorts.Count > 8) cohorts = cohorts.Skip(cohorts.Count - 8).ToList();

        var rows = new List<CohortRowDto>();
        foreach (var c in cohorts)
        {
            var members = perUser.Where(u => u.First == c).ToList();
            int size = members.Count;
            var retained = new int[maxOffset + 1];
            foreach (var m in members)
                for (int k = 0; k <= maxOffset; k++)
                    if (m.Months.Contains(c.AddMonths(k))) retained[k]++;
            rows.Add(new CohortRowDto
            {
                Cohort = c.ToString("MM/yyyy"),
                Size = size,
                Retained = retained.ToList(),
                RetainedPct = retained.Select(r => size == 0 ? 0 : Math.Round(r * 100.0 / size, 0)).ToList()
            });
        }
        return new CohortReportDto
        {
            OffsetLabels = Enumerable.Range(0, maxOffset + 1).Select(k => "Tháng " + k).ToList(),
            Rows = rows
        };
    }

    // Khung giờ vàng: đơn hoàn tất theo GIỜ (giờ VN) × THỨ trong tuần.
    public async Task<PeakTimeReportDto> GetPeakTimeAsync(DateTime from, DateTime to)
    {
        var orders = await Completed(from, to).Select(o => new { o.CreatedAt, o.TotalAmount }).ToListAsync();
        var cells = new Dictionary<(int wd, int h), (int cnt, decimal rev)>();
        var byHour = new int[24];
        var byWeekday = new int[7];
        foreach (var o in orders)
        {
            var local = o.CreatedAt.AddHours(7);       // đổi sang giờ Việt Nam
            int h = local.Hour;
            int wd = ((int)local.DayOfWeek + 6) % 7;    // 0 = Thứ 2 ... 6 = Chủ nhật
            var cur = cells.TryGetValue((wd, h), out var v) ? v : (0, 0m);
            cells[(wd, h)] = (cur.Item1 + 1, cur.Item2 + o.TotalAmount);
            byHour[h]++; byWeekday[wd]++;
        }
        var cellList = cells.Select(kv => new PeakCellDto { Weekday = kv.Key.wd, Hour = kv.Key.h, Orders = kv.Value.cnt, Revenue = kv.Value.rev }).ToList();
        var peak = cellList.OrderByDescending(c => c.Orders).FirstOrDefault();
        return new PeakTimeReportDto
        {
            Cells = cellList,
            MaxOrders = cellList.Count == 0 ? 0 : cellList.Max(c => c.Orders),
            PeakHour = peak?.Hour ?? 0,
            PeakWeekday = peak?.Weekday ?? 0,
            ByHour = byHour.ToList(),
            ByWeekday = byWeekday.ToList(),
            TotalOrders = orders.Count
        };
    }

    // Phân tích đánh giá: phân bố sao, xu hướng theo tháng, sản phẩm bị chê nhất.
    public async Task<ReviewReportDto> GetReviewReportAsync(DateTime from, DateTime to)
    {
        var reviews = await _db.Reviews.AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt < to)
            .Select(r => new { r.Rating, r.CreatedAt, r.ProductId, Name = r.Product.Name }).ToListAsync();

        var dist = new int[5]; // [5 sao, 4, 3, 2, 1]
        foreach (var r in reviews) if (r.Rating is >= 1 and <= 5) dist[5 - r.Rating]++;

        var trend = reviews
            .GroupBy(r => new DateTime(r.CreatedAt.Year, r.CreatedAt.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new ReviewTrendDto { Label = g.Key.ToString("MM/yyyy"), Count = g.Count(), Avg = Math.Round(g.Average(x => x.Rating), 2) })
            .ToList();

        var worst = reviews
            .GroupBy(r => new { r.ProductId, r.Name })
            .Select(g => new WorstProductDto { ProductId = g.Key.ProductId, ProductName = g.Key.Name, AvgRating = Math.Round(g.Average(x => x.Rating), 2), ReviewCount = g.Count() })
            .OrderBy(w => w.AvgRating).ThenByDescending(w => w.ReviewCount)
            .Take(10).ToList();

        return new ReviewReportDto
        {
            TotalReviews = reviews.Count,
            AvgRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 2),
            Distribution = dist.ToList(),
            Trend = trend,
            WorstProducts = worst
        };
    }

    // Hiệu quả Flash Sale: số lượng/doanh thu bán ra TRONG khung giờ sale + mức giảm đã cho.
    public async Task<FlashSaleReportDto> GetFlashSaleReportAsync()
    {
        var now = DateTime.UtcNow;
        var sales = await _db.FlashSales.AsNoTracking()
            .Include(f => f.Items).ThenInclude(i => i.Product).ToListAsync();
        if (sales.Count == 0) return new FlashSaleReportDto();

        var rows = new List<FlashSaleRowDto>();
        foreach (var s in sales)
            foreach (var it in s.Items)
            {
                var sold = await _db.OrderItems.AsNoTracking()
                    .Where(oi => oi.Variant.ProductId == it.ProductId
                        && oi.Order.Status != OrderStatus.Cancelled
                        && oi.Order.CreatedAt >= s.StartAt && oi.Order.CreatedAt <= s.EndAt)
                    .Select(oi => new { oi.Quantity, oi.PriceSnapshot }).ToListAsync();
                int units = sold.Sum(x => x.Quantity);
                if (units == 0) continue;
                var revenue = sold.Sum(x => x.PriceSnapshot * x.Quantity);
                rows.Add(new FlashSaleRowDto
                {
                    SaleName = s.Name,
                    ProductName = it.Product.Name,
                    OriginalPrice = it.Product.BasePrice,
                    FlashPrice = it.FlashPrice,
                    UnitsSold = units,
                    Revenue = revenue,
                    // Số tiền khách THỰC SỰ tiết kiệm = giá gốc × SL − doanh thu thực thu (>= 0).
                    Discount = Math.Max(0, it.Product.BasePrice * units - revenue),
                    Running = s.IsActive && s.StartAt <= now && s.EndAt > now
                });
            }
        rows = rows.OrderByDescending(r => r.Revenue).ToList();
        return new FlashSaleReportDto
        {
            Programs = sales.Count,
            UnitsSold = rows.Sum(r => r.UnitsSold),
            Revenue = rows.Sum(r => r.Revenue),
            DiscountGiven = rows.Sum(r => r.Discount),
            Items = rows
        };
    }
}
