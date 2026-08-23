using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace PhoneStore.Application.Services;

public class FlashSaleService : IFlashSaleService
{
    private readonly IAppDbContext _db;
    public FlashSaleService(IAppDbContext db) => _db = db;

    public async Task<FlashSaleDto?> GetActiveAsync()
    {
        var now = DateTime.UtcNow;
        var sale = await _db.FlashSales.AsNoTracking()
            .Include(f => f.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .Where(f => f.IsActive && f.StartAt <= now && f.EndAt > now)
            .OrderBy(f => f.EndAt).FirstOrDefaultAsync();
        return sale == null ? null : ToDto(sale, now);
    }

    public async Task<List<FlashSaleDto>> GetAllAsync()
    {
        var now = DateTime.UtcNow;
        var sales = await _db.FlashSales.AsNoTracking()
            .Include(f => f.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .OrderByDescending(f => f.StartAt).ToListAsync();
        return sales.Select(s => ToDto(s, now)).ToList();
    }

    public async Task<FlashSaleDto> CreateAsync(CreateFlashSaleDto dto)
    {
        var sale = new FlashSale { Name = dto.Name.Trim(), StartAt = dto.StartAt, EndAt = dto.EndAt, IsActive = dto.IsActive };
        foreach (var it in dto.Items)
            sale.Items.Add(new FlashSaleItem { ProductId = it.ProductId, FlashPrice = it.FlashPrice, QuantityLimit = it.QuantityLimit });
        _db.FlashSales.Add(sale);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(sale.Id))!;
    }

    public async Task<FlashSaleDto> UpdateAsync(int id, CreateFlashSaleDto dto)
    {
        var sale = await _db.FlashSales.Include(f => f.Items).FirstOrDefaultAsync(f => f.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy chương trình Flash Sale.");
        sale.Name = dto.Name.Trim(); sale.StartAt = dto.StartAt; sale.EndAt = dto.EndAt; sale.IsActive = dto.IsActive;
        _db.FlashSaleItems.RemoveRange(sale.Items);
        sale.Items.Clear();
        foreach (var it in dto.Items)
            sale.Items.Add(new FlashSaleItem { ProductId = it.ProductId, FlashPrice = it.FlashPrice, QuantityLimit = it.QuantityLimit });
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var sale = await _db.FlashSales.FindAsync(id) ?? throw AppException.NotFound("Không tìm thấy chương trình.");
        _db.FlashSales.Remove(sale);
        await _db.SaveChangesAsync();
    }

    private async Task<FlashSaleDto?> GetByIdAsync(int id)
    {
        var now = DateTime.UtcNow;
        var sale = await _db.FlashSales.AsNoTracking()
            .Include(f => f.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(f => f.Id == id);
        return sale == null ? null : ToDto(sale, now);
    }

    private static FlashSaleDto ToDto(FlashSale f, DateTime now) => new()
    {
        Id = f.Id, Name = f.Name, StartAt = f.StartAt, EndAt = f.EndAt, IsActive = f.IsActive,
        IsRunning = f.IsActive && f.StartAt <= now && f.EndAt > now,
        Items = f.Items.Select(i => new FlashSaleItemDto
        {
            Id = i.Id, ProductId = i.ProductId,
            ProductName = i.Product.Name, ProductSlug = i.Product.Slug,
            ProductImage = i.Product.Images.Where(im => im.IsPrimary).Select(im => im.Url).FirstOrDefault()
                ?? i.Product.Images.Select(im => im.Url).FirstOrDefault(),
            OriginalPrice = i.Product.BasePrice, FlashPrice = i.FlashPrice,
            DiscountPercent = i.Product.BasePrice > 0 ? (int)System.Math.Round((1 - (double)(i.FlashPrice / i.Product.BasePrice)) * 100) : 0,
            // "Đã bán" lấy từ SoldCount THẬT của sản phẩm (cộng dồn từ đơn hàng thật, tự tăng khi có
            // người mua) — không dùng số mock. Nhờ vậy khớp đúng số đã bán ở thẻ sản phẩm.
            QuantityLimit = i.QuantityLimit, SoldCount = i.Product.SoldCount
        }).ToList()
    };
}
