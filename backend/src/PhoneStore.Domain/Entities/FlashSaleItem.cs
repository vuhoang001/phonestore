using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Sản phẩm trong Flash Sale: giá flash, suất giới hạn, đã bán.</summary>
public class FlashSaleItem : BaseEntity
{
    public int FlashSaleId { get; set; }
    public int ProductId { get; set; }
    public decimal FlashPrice { get; set; }
    public int QuantityLimit { get; set; }
    public int SoldCount { get; set; }

    public FlashSale FlashSale { get; set; } = default!;
    public Product Product { get; set; } = default!;
}
