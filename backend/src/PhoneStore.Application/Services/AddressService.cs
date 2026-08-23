using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

public class AddressService : IAddressService
{
    private readonly IAppDbContext _db;
    public AddressService(IAppDbContext db) => _db = db;

    public async Task<List<AddressDto>> GetMineAsync(int userId)
    {
        var list = await _db.Addresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.Id)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<AddressDto> CreateAsync(int userId, CreateAddressDto dto)
    {
        var entity = new Address
        {
            UserId = userId,
            RecipientName = dto.RecipientName,
            Phone = dto.Phone,
            Province = dto.Province,
            District = dto.District,
            Ward = dto.Ward,
            Detail = dto.Detail,
            Note = dto.Note,
            IsDefault = dto.IsDefault
        };
        if (dto.IsDefault) await ClearDefault(userId);
        // Địa chỉ đầu tiên mặc định là default
        if (!await _db.Addresses.AnyAsync(a => a.UserId == userId)) entity.IsDefault = true;

        _db.Addresses.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<AddressDto> UpdateAsync(int userId, int id, CreateAddressDto dto)
    {
        var entity = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw AppException.NotFound("Không tìm thấy địa chỉ.");
        if (dto.IsDefault && !entity.IsDefault) await ClearDefault(userId);

        entity.RecipientName = dto.RecipientName;
        entity.Phone = dto.Phone;
        entity.Province = dto.Province;
        entity.District = dto.District;
        entity.Ward = dto.Ward;
        entity.Detail = dto.Detail;
        entity.Note = dto.Note;
        entity.IsDefault = dto.IsDefault;
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var entity = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw AppException.NotFound("Không tìm thấy địa chỉ.");
        _db.Addresses.Remove(entity);
        await _db.SaveChangesAsync();
    }

    private async Task ClearDefault(int userId)
    {
        var defaults = await _db.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
        foreach (var a in defaults) a.IsDefault = false;
    }

    private static AddressDto ToDto(Address a) => new()
    {
        Id = a.Id,
        RecipientName = a.RecipientName,
        Phone = a.Phone,
        Province = a.Province,
        District = a.District,
        Ward = a.Ward,
        Detail = a.Detail,
        Note = a.Note,
        IsDefault = a.IsDefault
    };
}
