using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

/// <summary>
/// Quản lý phiếu bảo hành theo IMEI (đặc thù điện thoại).
/// Phiếu được tạo khi admin gán IMEI cho từng máy lúc chuyển đơn sang Đang giao.
/// </summary>
public class WarrantyService : IWarrantyService
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    public WarrantyService(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<WarrantyLookupResultDto> LookupAsync(string? imei, string? orderCode)
    {
        imei = imei?.Trim();
        orderCode = orderCode?.Trim();
        if (string.IsNullOrWhiteSpace(imei) && string.IsNullOrWhiteSpace(orderCode))
            throw new AppException("Vui lòng nhập IMEI hoặc mã đơn hàng để tra cứu.");

        var query = _db.WarrantyRecords.AsNoTracking().Include(w => w.Order).AsQueryable();
        if (!string.IsNullOrWhiteSpace(imei))
            query = query.Where(w => w.Imei == imei);
        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            var code = orderCode.ToLower();
            query = query.Where(w => w.Order.OrderCode.ToLower() == code);
        }

        var records = await query.OrderByDescending(w => w.StartDate).ToListAsync();
        var items = records.Select(ToDto).ToList();
        return new WarrantyLookupResultDto(items.Count > 0, items);
    }

    public async Task AssignImeiAsync(int orderId, AssignImeiDto dto)
    {
        var imei = dto.Imei?.Trim();
        if (string.IsNullOrWhiteSpace(imei))
            throw new AppException("IMEI/Serial không được để trống.");

        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");
        var item = order.Items.FirstOrDefault(i => i.Id == dto.OrderItemId)
            ?? throw AppException.NotFound("Không tìm thấy dòng hàng trong đơn.");

        // IMEI phải là duy nhất toàn hệ thống (mỗi máy 1 số).
        if (await _db.WarrantyRecords.AnyAsync(w => w.Imei == imei))
            throw AppException.Conflict($"IMEI \"{imei}\" đã được sử dụng cho máy khác.");

        item.Imei = imei;

        // Số tháng bảo hành lấy từ cấu hình của máy (qua biến thể → sản phẩm), mặc định 12.
        var warrantyMonths = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.Id == item.VariantId)
            .Select(v => (int?)v.Product.WarrantyMonths)
            .FirstOrDefaultAsync() ?? 12;

        var start = DateTime.UtcNow;
        _db.WarrantyRecords.Add(new WarrantyRecord
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            Imei = imei,
            ProductNameSnapshot = item.ProductNameSnapshot,
            StartDate = start,
            EndDate = start.AddMonths(warrantyMonths),
            Status = WarrantyStatus.Active
        });

        await _audit.LogAsync("WarrantyCreated", "WarrantyRecord", item.Id, $"IMEI {imei} · đơn {order.OrderCode}");
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<WarrantyDto>> GetByOrderAsync(int orderId)
    {
        var records = await _db.WarrantyRecords.AsNoTracking()
            .Include(w => w.Order)
            .Where(w => w.OrderId == orderId)
            .OrderBy(w => w.StartDate)
            .ToListAsync();
        return records.Select(ToDto).ToList();
    }

    private static WarrantyDto ToDto(WarrantyRecord w) => new(
        w.Id, w.Imei, w.ProductNameSnapshot, w.Order?.OrderCode ?? string.Empty,
        w.StartDate, w.EndDate, w.Status.ToString());
}
