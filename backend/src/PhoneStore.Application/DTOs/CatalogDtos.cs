using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Application.DTOs;

// ---------- Category ----------
public record CategoryDto
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? ImageUrl { get; init; }
    public List<CategoryDto> Children { get; init; } = new();
}

public record CreateCategoryDto
{
    public int? ParentId { get; init; }
    [Required] public string Name { get; init; } = default!;
    public string? ImageUrl { get; init; }
}

// ---------- Brand (điện thoại) ----------
public record BrandDto(int Id, string Name, string Slug, string? LogoUrl, string? Description);

// ---------- Product ----------
public record ProductListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public decimal BasePrice { get; init; }
    public string? PrimaryImage { get; init; }
    /// <summary>Tên hãng (Apple, Samsung...) để hiển thị & lọc.</summary>
    public string BrandName { get; init; } = default!;
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TotalStock { get; init; }
    public int SoldCount { get; init; }
    public decimal? FlashPrice { get; init; } // Giá Flash Sale đang chạy (nếu có), null nếu không sale
}

public record ProductDetailDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = default!;
    public int BrandId { get; init; }
    public string BrandName { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public string Status { get; init; } = default!;
    /// <summary>Số tháng bảo hành mặc định của máy.</summary>
    public int WarrantyMonths { get; init; }
    /// <summary>Máy có hỗ trợ mua trả góp hay không.</summary>
    public bool InstallmentAvailable { get; init; }
    public List<ProductVariantDto> Variants { get; init; } = new();
    public List<ProductImageDto> Images { get; init; } = new();
    /// <summary>Bảng thông số kỹ thuật (gom nhóm theo Group ở phía UI).</summary>
    public IReadOnlyList<ProductSpecDto> Specifications { get; init; } = new List<ProductSpecDto>();
    /// <summary>Các kỳ hạn trả góp ước tính (chỉ có khi InstallmentAvailable = true).</summary>
    public IReadOnlyList<InstallmentOptionDto> InstallmentOptions { get; init; } = new List<InstallmentOptionDto>();
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int ViewCount { get; init; }
    public int SoldCount { get; init; }
    public List<int> RatingBreakdown { get; init; } = new(); // [5sao,4,3,2,1]
    public decimal? FlashPrice { get; init; }   // Giá Flash Sale đang chạy (nếu có)
    public DateTime? FlashEndAt { get; init; }   // Thời điểm kết thúc sale (để đếm ngược)
}

public record ProductVariantDto
{
    public int Id { get; init; }
    public string Sku { get; init; } = default!;
    public string? Color { get; init; }
    /// <summary>Mã màu hiển thị swatch (VD: #1e6fff).</summary>
    public string? ColorHex { get; init; }
    /// <summary>Dung lượng lưu trữ (VD: 128GB, 256GB).</summary>
    public string? Storage { get; init; }
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
}

public record ProductImageDto
{
    public int Id { get; init; }
    public int? VariantId { get; init; }
    public string Url { get; init; } = default!;
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}

/// <summary>Một dòng thông số kỹ thuật của máy.</summary>
public record ProductSpecDto(string Group, string Name, string Value);

/// <summary>Ước tính trả góp theo kỳ hạn (mock, lãi suất 0% cho demo — chia đều).</summary>
public record InstallmentOptionDto(int Months, decimal Monthly);

public record CreateProductDto
{
    [Required] public int CategoryId { get; init; }
    [Required] public int BrandId { get; init; }
    [Required] public string Name { get; init; } = default!;
    public string? Description { get; init; }
    [Range(0, double.MaxValue)] public decimal BasePrice { get; init; }
    public string Status { get; init; } = "Active";
    [Range(0, 120)] public int WarrantyMonths { get; init; } = 12;
    public bool InstallmentAvailable { get; init; }
    public List<CreateVariantDto> Variants { get; init; } = new();
    public List<CreateImageDto> Images { get; init; } = new();
    public List<ProductSpecInputDto> Specifications { get; init; } = new();
}

public record UpdateProductDto
{
    [Required] public int CategoryId { get; init; }
    [Required] public int BrandId { get; init; }
    [Required] public string Name { get; init; } = default!;
    public string? Description { get; init; }
    [Range(0, double.MaxValue)] public decimal BasePrice { get; init; }
    public string Status { get; init; } = "Active";
    [Range(0, 120)] public int WarrantyMonths { get; init; } = 12;
    public bool InstallmentAvailable { get; init; }
    // Danh sách biến thể sau khi sửa. Rỗng => giữ nguyên biến thể cũ (không đụng tồn kho).
    public List<UpdateVariantDto> Variants { get; init; } = new();
    // Danh sách thông số kỹ thuật sau khi sửa. Rỗng => giữ nguyên.
    public List<ProductSpecInputDto> Specifications { get; init; } = new();
}

public record CreateVariantDto
{
    public string? Sku { get; init; }
    public string? Color { get; init; }
    public string? ColorHex { get; init; }
    public string? Storage { get; init; }
    [Range(0, double.MaxValue)] public decimal Price { get; init; }
    [Range(0, double.MaxValue)] public decimal Cost { get; init; }
    [Range(0, int.MaxValue)] public int StockQuantity { get; init; }
}

public record UpdateVariantDto
{
    public int Id { get; init; }            // 0 = biến thể mới; >0 = cập nhật biến thể sẵn có
    public string? Sku { get; init; }
    public string? Color { get; init; }
    public string? ColorHex { get; init; }
    public string? Storage { get; init; }
    [Range(0, double.MaxValue)] public decimal Price { get; init; }
    [Range(0, double.MaxValue)] public decimal Cost { get; init; }
    [Range(0, int.MaxValue)] public int StockQuantity { get; init; }
}

public record CreateImageDto
{
    [Required] public string Url { get; init; } = default!;
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}

/// <summary>Input tạo/sửa một dòng thông số kỹ thuật.</summary>
public record ProductSpecInputDto(string Group, string Name, string Value);

/// <summary>Bộ lọc tìm kiếm sản phẩm phía client.</summary>
public record ProductFilterDto
{
    public string? Keyword { get; init; }
    public int? CategoryId { get; init; }
    public int? BrandId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinRating { get; init; }
    public string? SortBy { get; init; } // newest | price_asc | price_desc | rating
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}
