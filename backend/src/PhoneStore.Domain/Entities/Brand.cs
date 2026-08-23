using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Thương hiệu điện thoại (Apple, Samsung, Xiaomi, OPPO...). Dùng để lọc & báo cáo theo hãng.</summary>
public class Brand : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    /// <summary>Logo hãng (URL trên MinIO).</summary>
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
