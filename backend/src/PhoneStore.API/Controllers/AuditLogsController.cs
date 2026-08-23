using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.API.Controllers;

/// <summary>Nhật ký thao tác quan trọng — chỉ admin xem.</summary>
[Authorize(Roles = "Admin")]
public class AuditLogsController : BaseApiController
{
    private readonly IAppDbContext _db;
    public AuditLogsController(IAppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Get([FromQuery] PaginationQuery query)
    {
        var q = _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreatedAt);
        var total = await q.CountAsync();
        var items = await q
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Detail = a.Detail,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<AuditLogDto>
        {
            Items = items, Page = query.Page, PageSize = query.PageSize, TotalItems = total
        });
    }
}
