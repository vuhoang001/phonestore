using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

public class CouponService : ICouponService
{
    private readonly IAppDbContext _db;
    public CouponService(IAppDbContext db) => _db = db;

    public async Task<List<CouponDto>> GetAllAsync()
    {
        var list = await _db.Coupons.AsNoTracking().OrderByDescending(c => c.Id).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<List<CouponDto>> GetAvailableAsync()
    {
        var now = DateTime.UtcNow;
        var list = await _db.Coupons.AsNoTracking()
            .Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now && c.UsedCount < c.UsageLimit)
            .OrderBy(c => c.MinOrderAmount)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<CouponDto> CreateAsync(CreateCouponDto dto)
    {
        var code = dto.Code.Trim().ToUpper();
        if (await _db.Coupons.AnyAsync(c => c.Code.ToUpper() == code))
            throw AppException.Conflict("Mã giảm giá đã tồn tại.");

        var entity = new Coupon
        {
            Code = code,
            DiscountType = Enum.TryParse<DiscountType>(dto.DiscountType, true, out var dt) ? dt : DiscountType.Percentage,
            DiscountValue = dto.DiscountValue,
            MinOrderAmount = dto.MinOrderAmount,
            StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc),
            UsageLimit = dto.UsageLimit,
            IsActive = true
        };
        _db.Coupons.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.Coupons.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy mã giảm giá.");
        _db.Coupons.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<CouponDto> ValidateAsync(string code, decimal orderAmount)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == code.Trim().ToUpper())
            ?? throw AppException.NotFound("Mã giảm giá không tồn tại.");
        var now = DateTime.UtcNow;
        if (!coupon.IsActive || now < coupon.StartDate || now > coupon.EndDate)
            throw new AppException("Mã giảm giá đã hết hạn hoặc chưa có hiệu lực.");
        if (coupon.UsedCount >= coupon.UsageLimit)
            throw new AppException("Mã giảm giá đã hết lượt sử dụng.");
        if (orderAmount < coupon.MinOrderAmount)
            throw new AppException($"Đơn hàng tối thiểu {coupon.MinOrderAmount:N0}đ để dùng mã này.");
        return ToDto(coupon);
    }

    private static CouponDto ToDto(Coupon c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        DiscountType = c.DiscountType.ToString(),
        DiscountValue = c.DiscountValue,
        MinOrderAmount = c.MinOrderAmount,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        UsageLimit = c.UsageLimit,
        UsedCount = c.UsedCount,
        IsActive = c.IsActive
    };
}
