using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IAppDbContext _db;
    public CategoryService(IAppDbContext db) => _db = db;

    public async Task<List<CategoryDto>> GetTreeAsync()
    {
        var all = await _db.Categories.AsNoTracking().ToListAsync();
        var lookup = all.ToDictionary(c => c.Id, ToDto);
        var roots = new List<CategoryDto>();
        foreach (var c in all)
        {
            var dto = lookup[c.Id];
            if (c.ParentId is int pid && lookup.TryGetValue(pid, out var parent))
                parent.Children.Add(dto);
            else
                roots.Add(dto);
        }
        return roots;
    }

    public async Task<List<CategoryDto>> GetAllFlatAsync()
    {
        var all = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        return all.Select(ToDto).ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var entity = new Category
        {
            ParentId = dto.ParentId,
            Name = dto.Name.Trim(),
            Slug = await UniqueSlug(SlugHelper.Generate(dto.Name)),
            ImageUrl = dto.ImageUrl
        };
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<CategoryDto> UpdateAsync(int id, CreateCategoryDto dto)
    {
        var entity = await _db.Categories.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy danh mục.");
        entity.Name = dto.Name.Trim();
        entity.ParentId = dto.ParentId;
        entity.ImageUrl = dto.ImageUrl;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.Categories.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy danh mục.");
        if (await _db.Products.AnyAsync(p => p.CategoryId == id))
            throw new AppException("Không thể xóa danh mục đang có sản phẩm.");
        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();
    }

    private async Task<string> UniqueSlug(string baseSlug)
    {
        var slug = baseSlug;
        var i = 1;
        while (await _db.Categories.AnyAsync(c => c.Slug == slug))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        Id = c.Id,
        ParentId = c.ParentId,
        Name = c.Name,
        Slug = c.Slug,
        ImageUrl = c.ImageUrl
    };
}
