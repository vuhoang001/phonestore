using PhoneStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace PhoneStore.Application.Interfaces;

/// <summary>Trừu tượng hóa DbContext để tầng Application không phụ thuộc Infrastructure.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Address> Addresses { get; }
    DbSet<Brand> Brands { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductSpecification> ProductSpecifications { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderStatusHistory> OrderStatusHistories { get; }
    DbSet<Payment> Payments { get; }
    DbSet<ShippingMethod> ShippingMethods { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<OrderCoupon> OrderCoupons { get; }
    DbSet<Review> Reviews { get; }
    DbSet<ReviewImage> ReviewImages { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<WarrantyRecord> WarrantyRecords { get; }
    DbSet<TradeInRequest> TradeInRequests { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserToken> UserTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SearchLog> SearchLogs { get; }
    DbSet<FlashSale> FlashSales { get; }
    DbSet<FlashSaleItem> FlashSaleItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    /// <summary>Mở transaction để gom nhiều thao tác ghi thành nguyên tử (atomic).</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
