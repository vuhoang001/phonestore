using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

/// <summary>
/// Yêu cầu thu cũ đổi mới: khách khai máy cũ, admin định giá thu để trừ vào máy mới.
/// </summary>
public class TradeInService : ITradeInService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notif;
    public TradeInService(IAppDbContext db, INotificationService notif)
    {
        _db = db;
        _notif = notif;
    }

    public async Task<TradeInDto> CreateAsync(int userId, CreateTradeInDto dto)
    {
        if (dto.TargetProductId is int pid && !await _db.Products.AnyAsync(p => p.Id == pid))
            throw AppException.NotFound("Sản phẩm muốn đổi sang không tồn tại.");

        var entity = new TradeInRequest
        {
            UserId = userId,
            OldDeviceModel = dto.OldDeviceModel.Trim(),
            Condition = dto.Condition.Trim(),
            Note = dto.Note,
            QuotedPrice = 0,
            Status = TradeInStatus.Pending,
            TargetProductId = dto.TargetProductId
        };
        _db.TradeInRequests.Add(entity);
        await _db.SaveChangesAsync();

        // Báo admin có yêu cầu thu cũ mới cần định giá.
        await _notif.NotifyAdminsAsync("Yêu cầu thu cũ đổi mới",
            $"Khách gửi yêu cầu định giá máy cũ: {entity.OldDeviceModel}.", "system", "/admin/trade-in");

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<TradeInDto>> GetMineAsync(int userId)
    {
        var list = await _db.TradeInRequests.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<TradeInDto>> GetAllAsync()
    {
        var list = await _db.TradeInRequests.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<TradeInDto> QuoteAsync(int id, QuoteTradeInDto dto)
    {
        var entity = await _db.TradeInRequests.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy yêu cầu thu cũ.");
        if (entity.Status is TradeInStatus.Accepted or TradeInStatus.Rejected)
            throw new AppException("Yêu cầu đã kết thúc, không thể báo giá lại.");

        entity.QuotedPrice = dto.QuotedPrice;
        entity.Status = TradeInStatus.Quoted;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Báo khách đã có giá thu.
        await _notif.NotifyUserAsync(entity.UserId, "Báo giá thu cũ đổi mới",
            $"Máy \"{entity.OldDeviceModel}\" được định giá thu {dto.QuotedPrice:N0}đ.", "system", "/trade-in");

        return ToDto(entity);
    }

    public async Task<TradeInDto> UpdateStatusAsync(int id, string status)
    {
        var entity = await _db.TradeInRequests.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy yêu cầu thu cũ.");
        if (!Enum.TryParse<TradeInStatus>(status, true, out var newStatus))
            throw new AppException("Trạng thái không hợp lệ.");

        entity.Status = newStatus;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    private static TradeInDto ToDto(TradeInRequest t) => new(
        t.Id, t.OldDeviceModel, t.Condition, t.Note, t.QuotedPrice,
        t.Status.ToString(), t.TargetProductId, t.CreatedAt);
}
