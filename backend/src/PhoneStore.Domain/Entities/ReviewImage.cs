using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Ảnh đính kèm trong một đánh giá.</summary>
public class ReviewImage : BaseEntity
{
    public int ReviewId { get; set; }
    public string Url { get; set; } = default!;

    public Review Review { get; set; } = default!;
}
