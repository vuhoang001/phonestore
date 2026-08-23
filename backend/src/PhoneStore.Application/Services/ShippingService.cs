using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

/// <summary>Quản lý phương thức vận chuyển — admin tự tạo, không seed cứng.</summary>
public class ShippingService : IShippingService
{
    private readonly IAppDbContext _db;
    public ShippingService(IAppDbContext db) => _db = db;

    public async Task<List<ShippingMethodDto>> GetActiveAsync()
    {
        var list = await _db.ShippingMethods.AsNoTracking()
            .Where(m => m.IsActive).OrderBy(m => m.BaseFee).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<List<ShippingMethodDto>> GetAllAsync()
    {
        var list = await _db.ShippingMethods.AsNoTracking().OrderByDescending(m => m.Id).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ShippingMethodDto> CreateAsync(CreateShippingMethodDto dto)
    {
        var entity = new ShippingMethod
        {
            Name = dto.Name.Trim(),
            BaseFee = dto.BaseFee,
            EstimatedDays = dto.EstimatedDays,
            IsActive = dto.IsActive
        };
        _db.ShippingMethods.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<ShippingMethodDto> UpdateAsync(int id, CreateShippingMethodDto dto)
    {
        var entity = await _db.ShippingMethods.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy phương thức vận chuyển.");
        entity.Name = dto.Name.Trim();
        entity.BaseFee = dto.BaseFee;
        entity.EstimatedDays = dto.EstimatedDays;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.ShippingMethods.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy phương thức vận chuyển.");
        _db.ShippingMethods.Remove(entity);
        await _db.SaveChangesAsync();
    }

    private static ShippingMethodDto ToDto(ShippingMethod m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        BaseFee = m.BaseFee,
        EstimatedDays = m.EstimatedDays,
        IsActive = m.IsActive
    };
}
