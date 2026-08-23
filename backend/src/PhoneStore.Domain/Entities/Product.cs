using PhoneStore.Domain.Common;
using PhoneStore.Domain.Enums;

namespace PhoneStore.Domain.Entities;

/// <summary>Sản phẩm (điện thoại/phụ kiện). Giá &amp; tồn kho chi tiết nằm ở biến thể (ProductVariant).</summary>
public class Product : BaseEntity
{
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    /// <summary>Giá niêm yết tham chiếu (giá bán thực lấy theo biến thể rẻ nhất).</summary>
    public decimal BasePrice { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public int ViewCount { get; set; }
    public int SoldCount { get; set; }
    /// <summary>Số tháng bảo hành mặc định của máy (thường 12). Dùng khi tạo phiếu bảo hành lúc giao.</summary>
    public int WarrantyMonths { get; set; } = 12;
    /// <summary>Cho phép trả góp hay không (máy giá cao mới bật).</summary>
    public bool InstallmentAvailable { get; set; }

    public Category Category { get; set; } = default!;
    public Brand Brand { get; set; } = default!;
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
