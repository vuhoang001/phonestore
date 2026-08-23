using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Danh mục dạng cây (Điện thoại, Tablet, Phụ kiện, Đồng hồ...). Self-reference qua ParentId.</summary>
public class Category : BaseEntity
{
    public int? ParentId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? ImageUrl { get; set; }

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
