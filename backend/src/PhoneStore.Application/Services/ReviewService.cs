using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IAppDbContext _db;
    public ReviewService(IAppDbContext db) => _db = db;

    public async Task<List<ReviewDto>> GetByProductAsync(int productId)
    {
        var reviews = await _db.Reviews.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Images)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return reviews.Select(ToDto).ToList();
    }

    public async Task<ReviewDto> CreateAsync(int userId, CreateReviewDto dto)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == dto.ProductId))
            throw AppException.NotFound("Không tìm thấy sản phẩm.");

        // Chỉ cho đánh giá sản phẩm đã mua & đơn hoàn tất
        var hasPurchased = await _db.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Completed)
            .SelectMany(o => o.Items)
            .AnyAsync(i => i.Variant.ProductId == dto.ProductId);
        if (!hasPurchased)
            throw AppException.Forbidden("Bạn cần mua và nhận sản phẩm trước khi đánh giá.");

        // Mỗi người chỉ đánh giá một sản phẩm một lần.
        if (await _db.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == dto.ProductId))
            throw AppException.Conflict("Bạn đã đánh giá sản phẩm này rồi.");

        var review = new Review
        {
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Comment = dto.Comment
        };
        foreach (var url in dto.Images)
            review.Images.Add(new ReviewImage { Url = url });

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        var saved = await _db.Reviews.Include(r => r.User).Include(r => r.Images)
            .FirstAsync(r => r.Id == review.Id);
        return ToDto(saved);
    }

    public async Task<CanReviewDto> CanReviewAsync(int userId, int productId)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId))
            return new CanReviewDto { CanReview = false, Reason = "Sản phẩm không tồn tại." };

        var hasPurchased = await _db.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Completed)
            .SelectMany(o => o.Items)
            .AnyAsync(i => i.Variant.ProductId == productId);
        if (!hasPurchased)
            return new CanReviewDto { CanReview = false, Reason = "Bạn cần mua và nhận sản phẩm trước khi đánh giá." };

        if (await _db.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == productId))
            return new CanReviewDto { CanReview = false, Reason = "Bạn đã đánh giá sản phẩm này rồi." };

        return new CanReviewDto { CanReview = true };
    }

    public async Task DeleteAsync(int userId, string? role, int id)
    {
        var review = await _db.Reviews.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy đánh giá.");
        var isAdmin = role is nameof(UserRole.Admin);
        if (!isAdmin && review.UserId != userId)
            throw AppException.Forbidden("Bạn không có quyền xóa đánh giá này.");
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
    }

    private static ReviewDto ToDto(Review r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        UserName = r.User?.FullName ?? "Người dùng",
        Rating = r.Rating,
        Comment = r.Comment,
        Images = r.Images.Select(i => i.Url).ToList(),
        CreatedAt = r.CreatedAt
    };
}
