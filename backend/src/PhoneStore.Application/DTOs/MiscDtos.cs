using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Application.DTOs;

// ---------- Address ----------
public record AddressDto
{
    public int Id { get; init; }
    public string RecipientName { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string Province { get; init; } = default!;
    public string District { get; init; } = default!;
    public string Ward { get; init; } = default!;
    public string Detail { get; init; } = default!;
    public string? Note { get; init; }
    public bool IsDefault { get; init; }
}

public record CreateAddressDto
{
    [Required] public string RecipientName { get; init; } = default!;
    [Required] public string Phone { get; init; } = default!;
    [Required] public string Province { get; init; } = default!;
    public string District { get; init; } = ""; // Không bắt buộc: hệ hành chính 2 cấp (từ 01/7/2025) bỏ quận/huyện
    [Required] public string Ward { get; init; } = default!;
    [Required] public string Detail { get; init; } = default!;
    public string? Note { get; init; } // Ghi chú giao hàng (tùy chọn)
    public bool IsDefault { get; init; }
}

// ---------- Review ----------
public record ReviewDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string UserName { get; init; } = default!;
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public List<string> Images { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record CreateReviewDto
{
    [Required] public int ProductId { get; init; }
    [Range(1, 5)] public int Rating { get; init; }
    public string? Comment { get; init; }
    public List<string> Images { get; init; } = new();
}

public record CanReviewDto
{
    public bool CanReview { get; init; }
    public string Reason { get; init; } = "";
}

// ---------- Coupon ----------
public record CouponDto
{
    public int Id { get; init; }
    public string Code { get; init; } = default!;
    public string DiscountType { get; init; } = default!;
    public decimal DiscountValue { get; init; }
    public decimal MinOrderAmount { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int UsageLimit { get; init; }
    public int UsedCount { get; init; }
    public bool IsActive { get; init; }
}

public record CreateCouponDto
{
    [Required] public string Code { get; init; } = default!;
    public string DiscountType { get; init; } = "Percentage";
    [Range(0, double.MaxValue)] public decimal DiscountValue { get; init; }
    [Range(0, double.MaxValue)] public decimal MinOrderAmount { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    [Range(1, int.MaxValue)] public int UsageLimit { get; init; } = 100;
}

// ---------- Shipping ----------
public record ShippingMethodDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public decimal BaseFee { get; init; }
    public int EstimatedDays { get; init; }
    public bool IsActive { get; init; }
}

public record CreateShippingMethodDto
{
    [Required] public string Name { get; init; } = default!;
    [Range(0, double.MaxValue)] public decimal BaseFee { get; init; }
    [Range(0, int.MaxValue)] public int EstimatedDays { get; init; }
    public bool IsActive { get; init; } = true;
}

// ---------- Audit log ----------
public record AuditLogDto
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string Action { get; init; } = default!;
    public string EntityType { get; init; } = default!;
    public int? EntityId { get; init; }
    public string? Detail { get; init; }
    public DateTime CreatedAt { get; init; }
}

// ---------- Wishlist ----------
public record WishlistItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public decimal BasePrice { get; init; }
    public string? PrimaryImage { get; init; }
}

// ---------- Dashboard ----------
public record DashboardStatsDto
{
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public int TotalProducts { get; init; }
    public int TotalCustomers { get; init; }
    public List<RevenuePointDto> RevenueByDay { get; init; } = new();
    public List<TopProductDto> TopProducts { get; init; } = new();
    public Dictionary<string, int> OrdersByStatus { get; init; } = new();
}

public record RevenuePointDto
{
    public string Date { get; init; } = default!;
    public decimal Revenue { get; init; }
}

public record TopProductDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
}
