using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Chương trình Flash Sale theo khung giờ.</summary>
public class FlashSale : BaseEntity
{
    public string Name { get; set; } = default!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<FlashSaleItem> Items { get; set; } = new List<FlashSaleItem>();
}
