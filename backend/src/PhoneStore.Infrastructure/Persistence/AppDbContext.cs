using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Common;
using PhoneStore.Domain.Entities;

namespace PhoneStore.Infrastructure.Persistence;

/// <summary>EF Core DbContext — hiện thực IAppDbContext. Schema tạo bằng EnsureCreated lúc khởi động.</summary>
public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<OrderCoupon> OrderCoupons => Set<OrderCoupon>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WarrantyRecord> WarrantyRecords => Set<WarrantyRecord>();
    public DbSet<TradeInRequest> TradeInRequests => Set<TradeInRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SearchLog> SearchLogs => Set<SearchLog>();
    public DbSet<FlashSale> FlashSales => Set<FlashSale>();
    public DbSet<FlashSaleItem> FlashSaleItems => Set<FlashSaleItem>();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await Database.BeginTransactionAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---- Index & ràng buộc duy nhất ----
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Brand>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<ProductVariant>().HasIndex(x => x.Sku).IsUnique();
        b.Entity<Order>().HasIndex(x => x.OrderCode).IsUnique();
        b.Entity<Coupon>().HasIndex(x => x.Code).IsUnique();
        b.Entity<WarrantyRecord>().HasIndex(x => x.Imei).IsUnique();

        // ---- Khóa chính phức của bảng nối ----
        b.Entity<OrderCoupon>().HasKey(x => new { x.OrderId, x.CouponId });

        // ---- Precision cho tiền tệ (numeric 18,2) ----
        foreach (var p in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            p.SetPrecision(18);
            p.SetScale(2);
        }

        // ---- Quan hệ chính ----
        b.Entity<Product>()
            .HasOne(p => p.Brand).WithMany(br => br.Products)
            .HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Category>()
            .HasOne(c => c.Parent).WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Cart>().HasOne(c => c.User).WithOne(u => u.Cart)
            .HasForeignKey<Cart>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Order>().HasOne(o => o.Payment).WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<WarrantyRecord>()
            .HasOne(w => w.OrderItem).WithMany()
            .HasForeignKey(w => w.OrderItemId).OnDelete(DeleteBehavior.Restrict);

        // ---- Lọc mềm toàn cục: chỉ lấy bản ghi chưa xóa ----
        foreach (var et in b.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(et.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(et.ClrType);
                method.Invoke(null, new object[] { b });
            }
        }
    }

    /// <summary>Gắn global query filter loại bỏ bản ghi đã xóa mềm (DeletedAt != null).</summary>
    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder b) where TEntity : BaseEntity
        => b.Entity<TEntity>().HasQueryFilter(e => e.DeletedAt == null);

    /// <summary>Tự cập nhật UpdatedAt mỗi lần lưu.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
