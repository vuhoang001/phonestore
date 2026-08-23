using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Phương thức vận chuyển và phí cơ bản.</summary>
public class ShippingMethod : BaseEntity
{
    public string Name { get; set; } = default!;
    public decimal BaseFee { get; set; }
    public int EstimatedDays { get; set; }
    public bool IsActive { get; set; } = true;
}
