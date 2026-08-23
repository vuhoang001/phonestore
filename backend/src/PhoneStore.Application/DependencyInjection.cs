using PhoneStore.Application.Interfaces;
using PhoneStore.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace PhoneStore.Application;

/// <summary>Đăng ký các service nghiệp vụ của tầng Application.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IWarrantyService, WarrantyService>();
        services.AddScoped<ITradeInService, TradeInService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IFlashSaleService, FlashSaleService>();
        return services;
    }
}
