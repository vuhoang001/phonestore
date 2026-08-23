using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>
/// Thông số kỹ thuật của máy, gom theo nhóm để hiển thị bảng đẹp và phục vụ so sánh.
/// VD: Group="Màn hình", Name="Tần số quét", Value="120Hz".
/// </summary>
public class ProductSpecification : BaseEntity
{
    public int ProductId { get; set; }
    /// <summary>Nhóm thông số: Màn hình, Chip, Camera, Pin & Sạc, Kết nối...</summary>
    public string Group { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Value { get; set; } = default!;
    public int SortOrder { get; set; }

    public Product Product { get; set; } = default!;
}
