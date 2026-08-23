using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

/// <summary>Quản lý thương hiệu điện thoại (Apple, Samsung, Xiaomi...).</summary>
public class BrandService : IBrandService
{
    private readonly IAppDbContext _db;
    public BrandService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BrandDto>> GetAllAsync()
    {
        var list = await _db.Brands.AsNoTracking()
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<BrandDto> CreateAsync(BrandDto dto)
    {
        var name = dto.Name.Trim();
        if (await _db.Brands.AnyAsync(b => b.Name.ToLower() == name.ToLower()))
            throw AppException.Conflict("Thương hiệu đã tồn tại.");

        var entity = new Brand
        {
            Name = name,
            Slug = await UniqueSlug(SlugHelper.Generate(name)),
            LogoUrl = dto.LogoUrl,
            Description = dto.Description,
            // Đưa vào cuối danh sách nếu chưa có thứ tự cụ thể.
            SortOrder = (await _db.Brands.MaxAsync(b => (int?)b.SortOrder) ?? 0) + 1
        };
        _db.Brands.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<BrandDto> UpdateAsync(int id, BrandDto dto)
    {
        var entity = await _db.Brands.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy thương hiệu.");
        var name = dto.Name.Trim();
        if (!string.Equals(entity.Name, name, StringComparison.OrdinalIgnoreCase)
            && await _db.Brands.AnyAsync(b => b.Id != id && b.Name.ToLower() == name.ToLower()))
            throw AppException.Conflict("Thương hiệu đã tồn tại.");

        entity.Name = name;
        entity.LogoUrl = dto.LogoUrl;
        entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.Brands.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy thương hiệu.");
        if (await _db.Products.AnyAsync(p => p.BrandId == id))
            throw new AppException("Không thể xóa thương hiệu đang có sản phẩm.");
        _db.Brands.Remove(entity);
        await _db.SaveChangesAsync();
    }

    private async Task<string> UniqueSlug(string baseSlug)
    {
        var slug = baseSlug;
        var i = 1;
        while (await _db.Brands.AnyAsync(b => b.Slug == slug))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    private static BrandDto ToDto(Brand b) => new(b.Id, b.Name, b.Slug, b.LogoUrl, b.Description);
}
