using PhoneStore.Domain.Common;

namespace PhoneStore.Domain.Entities;

/// <summary>Nhật ký thao tác quan trọng (ai làm gì, khi nào) phục vụ truy vết.</summary>
public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public int? EntityId { get; set; }
    public string? Detail { get; set; }
}
