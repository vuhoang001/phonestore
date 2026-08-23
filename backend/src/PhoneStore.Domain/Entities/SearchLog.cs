using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Nhật ký từ khóa tìm kiếm nội bộ — phục vụ báo cáo tìm kiếm/no-result.</summary>
public class SearchLog : BaseEntity
{
    public string Keyword { get; set; } = default!;
    public int ResultCount { get; set; }
    public int? UserId { get; set; }
}
