namespace PhoneStore.Application.DTOs;

// ===== A · Tài chính =====
public record CouponPerfDto
{
    public string Code { get; init; } = default!;
    public int TimesUsed { get; init; }
    public decimal TotalDiscount { get; init; }
    public decimal RevenueGenerated { get; init; }
}
public record PromotionReportDto
{
    public decimal TotalDiscount { get; init; }
    public int OrdersWithCoupon { get; init; }
    public decimal RevenueWithCoupon { get; init; }
    public decimal AovWithCoupon { get; init; }
    public decimal AovWithoutCoupon { get; init; }
    public List<CouponPerfDto> Coupons { get; init; } = new();
}

public record PaymentRowDto
{
    public string Method { get; init; } = default!;
    public string Status { get; init; } = default!;
    public int Orders { get; init; }
    public decimal Amount { get; init; }
}
public record ReconciliationDto
{
    public decimal CodPending { get; init; }       // COD đang trên đường (đơn Shipping/Confirmed)
    public int CodPendingOrders { get; init; }
    public decimal CodCollected { get; init; }      // COD đã thu (Completed)
    public decimal OnlinePaid { get; init; }
    public decimal OnlinePending { get; init; }
    public List<PaymentRowDto> Rows { get; init; } = new();
}

/// <summary>1 đơn trong drill-down đối soát (nguồn tạo ra con số tổng).</summary>
public record ReconOrderDto
{
    public int Id { get; init; }
    public string OrderCode { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public string OrderStatus { get; init; } = default!;
    public string Method { get; init; } = default!;
    public string PayStatus { get; init; } = default!;
    public decimal Amount { get; init; }
}

/// <summary>1 đơn trong drill-down lợi nhuận (nguồn của doanh thu/giá vốn/LN gộp).</summary>
public record ProfitOrderDto
{
    public int Id { get; init; }
    public string OrderCode { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public decimal Revenue { get; init; }
    public decimal Cogs { get; init; }
    public decimal Profit { get; init; }
    public decimal Discount { get; init; }
    public decimal Shipping { get; init; }
}

/// <summary>1 đơn hoàn tất trong drill-down Tổng quan (nguồn của Doanh thu/Đơn/SP đã bán/Giảm giá).</summary>
public record SummaryOrderDto
{
    public int Id { get; init; }
    public string OrderCode { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = default!;
    public int ItemCount { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
}

/// <summary>1 khách mới trong drill-down Tổng quan (nguồn của "Khách mới").</summary>
public record NewCustomerDto
{
    public int UserId { get; init; }
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
}

/// <summary>1 phân loại (variant) trong drill-down Tồn kho (nguồn của giá trị/đơn vị tồn, sắp hết, hết hàng).</summary>
public record InventoryItemDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string Sku { get; init; } = default!;
    public string Variant { get; init; } = default!;
    public int Stock { get; init; }
    public decimal Price { get; init; }
    public decimal Value { get; init; }
}

public record ProfitCategoryDto
{
    public string Category { get; init; } = default!;
    public decimal Revenue { get; init; }
    public decimal Cogs { get; init; }
    public decimal Profit { get; init; }
    public decimal MarginPct { get; init; }
}
public record ProfitReportDto
{
    public decimal Revenue { get; init; }
    public decimal Cogs { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal GrossMarginPct { get; init; }
    public decimal Discounts { get; init; }
    public decimal ShippingCharged { get; init; }
    public List<ProfitCategoryDto> ByCategory { get; init; } = new();
}

// ===== B · Khách hàng =====
public record ChurnCustomerDto
{
    public int UserId { get; init; }
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public DateTime? LastOrder { get; init; }
    public int DaysSince { get; init; }
    public decimal TotalSpent { get; init; }
}
public record ChurnReportDto
{
    public int TotalCustomers { get; init; }
    public int Active { get; init; }      // đặt hàng ≤30 ngày
    public int AtRisk { get; init; }      // 31–90 ngày
    public int Churned { get; init; }     // >90 ngày
    public int NeverOrdered { get; init; }
    public List<ChurnCustomerDto> TopChurned { get; init; } = new();
}

public record ProductPairDto
{
    public string ProductA { get; init; } = default!;
    public string ProductB { get; init; } = default!;
    public int Count { get; init; }
}

public record RfmSegmentDto
{
    public string Segment { get; init; } = default!;
    public int Customers { get; init; }
    public decimal Revenue { get; init; }
}
public record RfmReportDto
{
    public List<RfmSegmentDto> Segments { get; init; } = new();
}

// ===== C · Vận hành =====
public record DemandItemDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string Sku { get; init; } = default!;
    public int Stock { get; init; }
    public double AvgDailySold { get; init; }
    public double DaysLeft { get; init; }
    public int SuggestedReorder { get; init; }
}
public record ProcessingTimeDto
{
    public double AvgConfirmHours { get; init; }
    public double AvgShipHours { get; init; }
    public double AvgCompleteHours { get; init; }
    public double AvgTotalHours { get; init; }
    public int SampleSize { get; init; }
}
/// <summary>1 đơn mẫu trong drill-down thời gian xử lý (giờ ở từng chặng).</summary>
public record ProcessingOrderDto
{
    public int Id { get; init; }
    public string OrderCode { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public double ConfirmHours { get; init; }
    public double ShipHours { get; init; }
    public double CompleteHours { get; init; }
    public double TotalHours { get; init; }
}

public record CancelReasonDto
{
    public string Reason { get; init; } = default!;
    public int Count { get; init; }
    public decimal LostRevenue { get; init; }
}

// ===== D · Hành vi & tìm kiếm =====
public record ViewToSaleDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public int Views { get; init; }
    public int Sold { get; init; }
    public double ConversionRate { get; init; } // %
}
public record KeywordStatDto
{
    public string Keyword { get; init; } = default!;
    public int Count { get; init; }
    public int AvgResults { get; init; }
}
public record SearchReportDto
{
    public int TotalSearches { get; init; }
    public List<KeywordStatDto> TopKeywords { get; init; } = new();
    public List<KeywordStatDto> NoResultKeywords { get; init; } = new();
}
public record FunnelReportDto
{
    public int ProductViews { get; init; }
    public int CartItems { get; init; }
    public int Orders { get; init; }
    public int CompletedOrders { get; init; }
    public double ViewToOrderRate { get; init; }
    public double OrderCompletionRate { get; init; }
}

// ============ Báo cáo mở rộng ============

// Cohort — giữ chân khách theo tháng mua đầu
public record CohortReportDto
{
    public List<string> OffsetLabels { get; init; } = new(); // "Tháng 0","Tháng 1"...
    public List<CohortRowDto> Rows { get; init; } = new();
}
public record CohortRowDto
{
    public string Cohort { get; init; } = default!;      // "06/2026"
    public int Size { get; init; }                        // số khách mới của tháng
    public List<int> Retained { get; init; } = new();     // số khách quay lại theo offset
    public List<double> RetainedPct { get; init; } = new();
}

// Khung giờ vàng — đơn theo giờ (0-23) × thứ (0=Thứ 2 .. 6=CN)
public record PeakTimeReportDto
{
    public List<PeakCellDto> Cells { get; init; } = new();
    public int MaxOrders { get; init; }
    public int PeakHour { get; init; }
    public int PeakWeekday { get; init; }
    public List<int> ByHour { get; init; } = new();
    public List<int> ByWeekday { get; init; } = new();
    public int TotalOrders { get; init; }
}
public record PeakCellDto
{
    public int Weekday { get; init; }
    public int Hour { get; init; }
    public int Orders { get; init; }
    public decimal Revenue { get; init; }
}

// Phân tích đánh giá
public record ReviewReportDto
{
    public int TotalReviews { get; init; }
    public double AvgRating { get; init; }
    public List<int> Distribution { get; init; } = new(); // [5 sao, 4, 3, 2, 1]
    public List<ReviewTrendDto> Trend { get; init; } = new();
    public List<WorstProductDto> WorstProducts { get; init; } = new();
}
public record ReviewTrendDto { public string Label { get; init; } = default!; public int Count { get; init; } public double Avg { get; init; } }
public record WorstProductDto { public int ProductId { get; init; } public string ProductName { get; init; } = default!; public double AvgRating { get; init; } public int ReviewCount { get; init; } }

// Hiệu quả Flash Sale
public record FlashSaleReportDto
{
    public int Programs { get; init; }
    public int UnitsSold { get; init; }
    public decimal Revenue { get; init; }
    public decimal DiscountGiven { get; init; }
    public List<FlashSaleRowDto> Items { get; init; } = new();
}
public record FlashSaleRowDto
{
    public string SaleName { get; init; } = default!;
    public string ProductName { get; init; } = default!;
    public decimal OriginalPrice { get; init; }
    public decimal FlashPrice { get; init; }
    public int UnitsSold { get; init; }
    public decimal Revenue { get; init; }
    public decimal Discount { get; init; }
    public bool Running { get; init; }
}
