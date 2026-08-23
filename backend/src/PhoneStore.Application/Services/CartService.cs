using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

public class CartService : ICartService
{
    private readonly IAppDbContext _db;
    public CartService(IAppDbContext db) => _db = db;

    public async Task<CartDto> GetAsync(CartOwner owner)
    {
        var cart = await LoadCart(owner);
        return ToDto(cart, await FlashPricesForAsync(cart));
    }

    // Giá Flash Sale đang chạy cho các sản phẩm trong giỏ → giỏ hiển thị đúng giá sẽ thu.
    private async Task<Dictionary<int, decimal>> FlashPricesForAsync(Cart cart)
    {
        var productIds = cart.Items.Select(i => i.Variant.ProductId).Distinct().ToList();
        if (productIds.Count == 0) return new();
        var now = DateTime.UtcNow;
        return await _db.FlashSaleItems.AsNoTracking()
            .Where(fi => productIds.Contains(fi.ProductId)
                && fi.FlashSale.IsActive && fi.FlashSale.StartAt <= now && fi.FlashSale.EndAt > now)
            .GroupBy(fi => fi.ProductId)
            .Select(g => new { ProductId = g.Key, Price = g.Min(x => x.FlashPrice) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Price);
    }

    public async Task<CartDto> AddAsync(CartOwner owner, AddToCartDto dto)
    {
        if (dto.Quantity < 1)
            throw new AppException("Số lượng phải lớn hơn 0.");

        var cart = await LoadCart(owner);
        var variant = await _db.ProductVariants.FindAsync(dto.VariantId)
            ?? throw AppException.NotFound("Không tìm thấy biến thể sản phẩm.");

        var item = cart.Items.FirstOrDefault(i => i.VariantId == dto.VariantId);
        var newQty = (item?.Quantity ?? 0) + dto.Quantity;
        if (newQty > variant.StockQuantity)
            throw new AppException($"Chỉ còn {variant.StockQuantity} sản phẩm trong kho.");

        if (item is null)
            cart.Items.Add(new CartItem { CartId = cart.Id, VariantId = dto.VariantId, Quantity = dto.Quantity });
        else
            item.Quantity = newQty;

        await _db.SaveChangesAsync();
        return await GetAsync(owner);
    }

    public async Task<CartDto> UpdateItemAsync(CartOwner owner, int itemId, UpdateCartItemDto dto)
    {
        var cart = await LoadCart(owner);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw AppException.NotFound("Không tìm thấy dòng giỏ hàng.");

        if (dto.Quantity <= 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            var variant = await _db.ProductVariants.FindAsync(item.VariantId);
            if (variant != null && dto.Quantity > variant.StockQuantity)
                throw new AppException($"Chỉ còn {variant.StockQuantity} sản phẩm trong kho.");
            item.Quantity = dto.Quantity;
        }
        await _db.SaveChangesAsync();
        return await GetAsync(owner);
    }

    public async Task<CartDto> RemoveItemAsync(CartOwner owner, int itemId)
    {
        var cart = await LoadCart(owner);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }
        return await GetAsync(owner);
    }

    public async Task ClearAsync(CartOwner owner)
    {
        var cart = await LoadCart(owner);
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();
    }

    public async Task<CartDto> MergeAsync(int userId, string guestToken)
    {
        var guestCart = await _db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.GuestToken == guestToken);
        var userCart = await LoadCart(CartOwner.ForUser(userId));

        if (guestCart != null)
        {
            foreach (var gi in guestCart.Items)
            {
                var variant = await _db.ProductVariants.FindAsync(gi.VariantId);
                if (variant is null) continue;
                var existing = userCart.Items.FirstOrDefault(i => i.VariantId == gi.VariantId);
                if (existing is null)
                    userCart.Items.Add(new CartItem
                    {
                        CartId = userCart.Id,
                        VariantId = gi.VariantId,
                        Quantity = Math.Min(gi.Quantity, variant.StockQuantity)
                    });
                else
                    existing.Quantity = Math.Min(existing.Quantity + gi.Quantity, variant.StockQuantity);
            }
            _db.Carts.Remove(guestCart); // xóa giỏ khách sau khi gộp
            await _db.SaveChangesAsync();
        }
        return await GetAsync(CartOwner.ForUser(userId));
    }

    private async Task<Cart> LoadCart(CartOwner owner)
    {
        var query = _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Images);

        var cart = owner.IsGuest
            ? await query.FirstOrDefaultAsync(c => c.GuestToken == owner.GuestToken)
            : await query.FirstOrDefaultAsync(c => c.UserId == owner.UserId);

        if (cart is null)
        {
            cart = owner.IsGuest
                ? new Cart { GuestToken = owner.GuestToken }
                : new Cart { UserId = owner.UserId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }
        return cart;
    }

    private static CartDto ToDto(Cart cart, Dictionary<int, decimal> flashPrices)
    {
        var items = cart.Items.Select(i =>
        {
            var product = i.Variant.Product;
            var img = product?.Images.FirstOrDefault(im => im.IsPrimary)?.Url
                ?? product?.Images.FirstOrDefault()?.Url;
            var variantInfo = string.Join(" / ", new[] { i.Variant.Color, i.Variant.Storage }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            // Giá thực: flash nếu đang sale & rẻ hơn giá gốc, ngược lại giá variant.
            var price = flashPrices.TryGetValue(i.Variant.ProductId, out var fp) && fp < i.Variant.Price
                ? fp : i.Variant.Price;
            return new CartItemDto
            {
                Id = i.Id,
                VariantId = i.VariantId,
                ProductId = product?.Id ?? 0,
                ProductName = product?.Name ?? string.Empty,
                VariantInfo = string.IsNullOrEmpty(variantInfo) ? null : variantInfo,
                ImageUrl = img,
                Price = price,
                Quantity = i.Quantity,
                StockQuantity = i.Variant.StockQuantity,
                LineTotal = price * i.Quantity
            };
        }).ToList();

        return new CartDto
        {
            Id = cart.Id,
            Items = items,
            SubTotal = items.Sum(i => i.LineTotal),
            TotalQuantity = items.Sum(i => i.Quantity)
        };
    }
}
