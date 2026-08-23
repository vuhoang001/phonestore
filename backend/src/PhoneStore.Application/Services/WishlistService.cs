using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IAppDbContext _db;
    public WishlistService(IAppDbContext db) => _db = db;

    public async Task<List<WishlistItemDto>> GetMineAsync(int userId)
    {
        return await _db.Wishlists.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => new WishlistItemDto
            {
                Id = w.Id,
                ProductId = w.ProductId,
                ProductName = w.Product.Name,
                Slug = w.Product.Slug,
                BasePrice = w.Product.BasePrice,
                PrimaryImage = w.Product.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()
                    ?? w.Product.Images.Select(i => i.Url).FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task ToggleAsync(int userId, int productId)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId))
            throw AppException.NotFound("Không tìm thấy sản phẩm.");

        var existing = await _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (existing != null)
            _db.Wishlists.Remove(existing);
        else
            _db.Wishlists.Add(new Wishlist { UserId = userId, ProductId = productId });
        await _db.SaveChangesAsync();
    }
}
