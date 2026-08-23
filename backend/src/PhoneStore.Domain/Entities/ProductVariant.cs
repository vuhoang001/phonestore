using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>
/// Biến thể máy theo tổ hợp Màu × Dung lượng lưu trữ (VD: Đen/128GB).
/// Giữ SKU, giá bán, giá vốn và tồn kho riêng cho từng tổ hợp.
/// </summary>
public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = default!;
    /// <summary>Màu sắc (VD: Titan Tự Nhiên, Xanh Dương).</summary>
    public string? Color { get; set; }
    /// <summary>Mã màu hiển thị swatch trên UI (VD: #1e6fff).</summary>
    public string? ColorHex { get; set; }
    /// <summary>Dung lượng lưu trữ (VD: 128GB, 256GB, 512GB, 1TB).</summary>
    public string? Storage { get; set; }
    public decimal Price { get; set; }
    public decimal Cost { get; set; } // giá vốn — dùng tính lợi nhuận gộp
    public int StockQuantity { get; set; }

    public Product Product { get; set; } = default!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
