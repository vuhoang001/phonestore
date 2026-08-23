using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Đánh giá sản phẩm của người dùng (chỉ khách đã mua mới được tạo).</summary>
public class Review : BaseEntity
{
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; } // 1..5
    public string? Comment { get; set; }

    public Product Product { get; set; } = default!;
    public User User { get; set; } = default!;
    public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
}
