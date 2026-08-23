namespace PhoneStore.Domain.Common;

/// <summary>Lớp cơ sở cho mọi entity, chứa khóa chính và dấu thời gian.</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    /// <summary>Thời điểm xóa mềm. Null = còn hiệu lực.</summary>
    public DateTime? DeletedAt { get; set; }
}
