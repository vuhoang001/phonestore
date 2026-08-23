using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Domain.Entities;

namespace PhoneStore.Application.Interfaces;

/// <summary>Băm và xác thực mật khẩu.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Gửi email (thật qua SMTP hoặc ghi log ở môi trường demo).</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
}

/// <summary>Lưu file (ảnh) lên kho đối tượng (MinIO/S3) và trả về URL công khai.</summary>
public interface IFileStorage
{
    Task<string> SaveImageAsync(Stream content, long length, string originalFileName, string contentType,
        string folder = "uploads", CancellationToken ct = default);
}

/// <summary>Ghi nhật ký thao tác quan trọng (kèm người thực hiện lấy từ ngữ cảnh hiện tại).</summary>
public interface IAuditLogger
{
    /// <summary>Thêm bản ghi audit vào context (được lưu cùng SaveChanges của caller).</summary>
    Task LogAsync(string action, string entityType, int? entityId = null, string? detail = null);
}

/// <summary>Sinh JWT cho người dùng.</summary>
public interface IJwtTokenGenerator
{
    (string token, DateTime expiresAt) Generate(User user);
}

/// <summary>Thông tin người dùng hiện tại lấy từ JWT của request.</summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserDto> GetProfileAsync(int userId);
    Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
    /// <summary>Đổi refresh token lấy cặp token mới (có xoay vòng token).</summary>
    Task<AuthResponseDto> RefreshAsync(string refreshToken);
    /// <summary>Thu hồi refresh token (đăng xuất).</summary>
    Task LogoutAsync(string? refreshToken);
    Task ConfirmEmailAsync(string token);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(string token, string newPassword);
}

public interface ICategoryService
{
    Task<List<CategoryDto>> GetTreeAsync();
    Task<List<CategoryDto>> GetAllFlatAsync();
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task<CategoryDto> UpdateAsync(int id, CreateCategoryDto dto);
    Task DeleteAsync(int id);
}

/// <summary>Quản lý thương hiệu điện thoại (Apple, Samsung...).</summary>
public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> GetAllAsync();
    Task<BrandDto> CreateAsync(BrandDto dto);
    Task<BrandDto> UpdateAsync(int id, BrandDto dto);
    Task DeleteAsync(int id);
}

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> SearchAsync(ProductFilterDto filter);
    Task<ProductDetailDto> GetBySlugAsync(string slug);
    Task<ProductDetailDto> GetByIdAsync(int id);
    Task<List<ProductListItemDto>> GetRelatedAsync(int productId);
    Task<ProductDetailDto> CreateAsync(CreateProductDto dto);
    Task<ProductDetailDto> UpdateAsync(int id, UpdateProductDto dto);
    Task DeleteAsync(int id);
}

public interface ICartService
{
    Task<CartDto> GetAsync(CartOwner owner);
    Task<CartDto> AddAsync(CartOwner owner, AddToCartDto dto);
    Task<CartDto> UpdateItemAsync(CartOwner owner, int itemId, UpdateCartItemDto dto);
    Task<CartDto> RemoveItemAsync(CartOwner owner, int itemId);
    Task ClearAsync(CartOwner owner);
    /// <summary>Gộp giỏ khách (guest) vào giỏ user khi đăng nhập.</summary>
    Task<CartDto> MergeAsync(int userId, string guestToken);
}

public interface IOrderService
{
    Task<OrderDto> CreateAsync(int userId, CreateOrderDto dto);
    Task<PagedResult<OrderDto>> GetMyOrdersAsync(int userId, PaginationQuery query, string? status);
    Task<OrderDto> GetByIdAsync(int userId, string? role, int orderId);
    Task<PagedResult<OrderDto>> GetAllAsync(PaginationQuery query, string? status,
        DateTime? from = null, DateTime? to = null, string? keyword = null);
    Task<OrderDto> UpdateStatusAsync(int orderId, UpdateOrderStatusDto dto);
    Task<OrderDto> CancelByCustomerAsync(int userId, int orderId, string? reason = null);
    /// <summary>Tự động hủy đơn Pending quá hạn chưa thanh toán. Trả về số đơn đã hủy.</summary>
    Task<int> CancelExpiredAsync(TimeSpan olderThan);
}

public interface IPaymentService
{
    /// <summary>Tạo URL thanh toán VNPAY cho một đơn hàng đang chờ thanh toán.</summary>
    Task<CreatePaymentDto> CreateVnPayUrlAsync(int userId, int orderId, string ipAddress);
    /// <summary>Xác thực callback từ VNPAY, cập nhật trạng thái thanh toán/đơn hàng.</summary>
    Task<PaymentResultDto> HandleVnPayReturnAsync(IReadOnlyDictionary<string, string> query);
    /// <summary>Hoàn tất thanh toán ở chế độ giả lập (chưa có credential VNPAY thật).</summary>
    Task<PaymentResultDto> CompleteMockAsync(int userId, int orderId, bool success);
}

public interface IAddressService
{
    Task<List<AddressDto>> GetMineAsync(int userId);
    Task<AddressDto> CreateAsync(int userId, CreateAddressDto dto);
    Task<AddressDto> UpdateAsync(int userId, int id, CreateAddressDto dto);
    Task DeleteAsync(int userId, int id);
}

public interface IReviewService
{
    Task<List<ReviewDto>> GetByProductAsync(int productId);
    Task<ReviewDto> CreateAsync(int userId, CreateReviewDto dto);
    Task DeleteAsync(int userId, string? role, int id);
    /// <summary>Người dùng có đủ điều kiện đánh giá sản phẩm này không (đã mua, chưa đánh giá).</summary>
    Task<CanReviewDto> CanReviewAsync(int userId, int productId);
}

public interface IWishlistService
{
    Task<List<WishlistItemDto>> GetMineAsync(int userId);
    Task ToggleAsync(int userId, int productId);
}

public interface ICouponService
{
    Task<List<CouponDto>> GetAllAsync();
    Task<List<CouponDto>> GetAvailableAsync();
    Task<CouponDto> CreateAsync(CreateCouponDto dto);
    Task DeleteAsync(int id);
    Task<CouponDto> ValidateAsync(string code, decimal orderAmount);
}

public interface IShippingService
{
    /// <summary>Danh sách phương thức đang bật (cho trang checkout).</summary>
    Task<List<ShippingMethodDto>> GetActiveAsync();
    /// <summary>Toàn bộ phương thức (cho admin).</summary>
    Task<List<ShippingMethodDto>> GetAllAsync();
    Task<ShippingMethodDto> CreateAsync(CreateShippingMethodDto dto);
    Task<ShippingMethodDto> UpdateAsync(int id, CreateShippingMethodDto dto);
    Task DeleteAsync(int id);
}

public interface INotificationService
{
    Task<NotificationListDto> GetMineAsync(int userId);
    Task MarkReadAsync(int userId, int id);
    Task MarkAllReadAsync(int userId);
    /// <summary>Tạo + lưu + đẩy realtime 1 thông báo cho 1 user.</summary>
    Task NotifyUserAsync(int userId, string title, string message, string type = "order", string? link = null);
    /// <summary>Tạo + lưu + đẩy realtime 1 thông báo cho TẤT CẢ admin.</summary>
    Task NotifyAdminsAsync(string title, string message, string type = "order", string? link = null);
}

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
}

public interface IFlashSaleService
{
    Task<FlashSaleDto?> GetActiveAsync();
    Task<List<FlashSaleDto>> GetAllAsync();
    Task<FlashSaleDto> CreateAsync(CreateFlashSaleDto dto);
    Task<FlashSaleDto> UpdateAsync(int id, CreateFlashSaleDto dto);
    Task DeleteAsync(int id);
}

/// <summary>Quản lý phiếu bảo hành theo IMEI (đặc thù điện thoại).</summary>
public interface IWarrantyService
{
    /// <summary>Tra cứu bảo hành theo IMEI hoặc mã đơn (cho phép ẩn danh).</summary>
    Task<WarrantyLookupResultDto> LookupAsync(string? imei, string? orderCode);
    /// <summary>Admin gán IMEI cho một dòng hàng và tạo phiếu bảo hành.</summary>
    Task AssignImeiAsync(int orderId, AssignImeiDto dto);
    /// <summary>Danh sách phiếu bảo hành của một đơn.</summary>
    Task<IReadOnlyList<WarrantyDto>> GetByOrderAsync(int orderId);
}

/// <summary>Quản lý yêu cầu thu cũ đổi mới.</summary>
public interface ITradeInService
{
    Task<TradeInDto> CreateAsync(int userId, CreateTradeInDto dto);
    Task<IReadOnlyList<TradeInDto>> GetMineAsync(int userId);
    Task<IReadOnlyList<TradeInDto>> GetAllAsync();       // admin
    Task<TradeInDto> QuoteAsync(int id, QuoteTradeInDto dto); // admin báo giá
    Task<TradeInDto> UpdateStatusAsync(int id, string status);
}
