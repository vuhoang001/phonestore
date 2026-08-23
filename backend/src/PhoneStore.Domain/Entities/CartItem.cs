using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Dòng sản phẩm trong giỏ hàng (theo biến thể màu × dung lượng).</summary>
public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }

    public Cart Cart { get; set; } = default!;
    public ProductVariant Variant { get; set; } = default!;
}
