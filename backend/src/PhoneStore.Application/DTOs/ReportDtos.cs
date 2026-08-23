namespace PhoneStore.Application.DTOs;

/// <summary>Một điểm trên trục thời gian (doanh thu + số đơn).</summary>
public record TimePointDto
{
    public string Label { get; init; } = default!;
    public decimal Revenue { get; init; }
    public int Orders { get; init; }
}

/// <summary>KPI tổng hợp cho khoảng thời gian.</summary>
public record ReportSummaryDto
{
    public decimal Revenue { get; init; }
    public int Orders { get; init; }
    public decimal AvgOrderValue { get; init; }
    public int ItemsSold { get; init; }
    public decimal Discounts { get; init; }
    public int NewCustomers { get; init; }
}

/// <summary>Báo cáo doanh thu theo thời gian (ngày/tháng).</summary>
public record RevenueReportDto
{
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public decimal AvgOrderValue { get; init; }
    public List<TimePointDto> Series { get; init; } = new();
}

public record TopProductReportDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
}

public record CategoryRevenueDto
{
    public string Category { get; init; } = default!;
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
}

public record PaymentMethodRevenueDto
{
    public string Method { get; init; } = default!;
    public int Orders { get; init; }
    public decimal Revenue { get; init; }
}

public record OrderStatsDto
{
    public int Total { get; init; }
    public Dictionary<string, int> ByStatus { get; init; } = new();
    public int Completed { get; init; }
    public int Cancelled { get; init; }
    public decimal CompletionRate { get; init; } // %
    public decimal CancelRate { get; init; }     // %
    public decimal AvgOrderValue { get; init; }
}

public record CountPointDto
{
    public string Label { get; init; } = default!;
    public int Count { get; init; }
}

public record LowStockItemDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string Sku { get; init; } = default!;
    public string? Variant { get; init; }
    public int Stock { get; init; }
}

public record InventoryReportDto
{
    public int TotalProducts { get; init; }
    public int TotalVariants { get; init; }
    public int TotalStockUnits { get; init; }
    public decimal StockValue { get; init; }
    public int OutOfStockCount { get; init; }
    public int LowStockCount { get; init; }
    public List<LowStockItemDto> LowStockItems { get; init; } = new();
}
