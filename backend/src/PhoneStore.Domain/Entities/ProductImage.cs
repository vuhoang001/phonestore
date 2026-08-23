using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Ảnh của sản phẩm, có thể gắn với một biến thể (màu) cụ thể.</summary>
public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string Url { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }

    public Product Product { get; set; } = default!;
    public ProductVariant? Variant { get; set; }
}
