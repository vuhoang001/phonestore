using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;

namespace PhoneStore.Application.Services;

/// <summary>Ghi audit log kèm người thực hiện (lấy từ ICurrentUser). Lưu cùng SaveChanges của caller.</summary>
public class AuditLogger : IAuditLogger
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditLogger(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task LogAsync(string action, string entityType, int? entityId = null, string? detail = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail
        });
        return Task.CompletedTask;
    }
}
