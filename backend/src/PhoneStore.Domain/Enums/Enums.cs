namespace PhoneStore.Domain.Enums;

/// <summary>Vai trò người dùng trong hệ thống.</summary>
public enum UserRole
{
    Customer = 0,
    Admin = 2
}

/// <summary>Trạng thái hiển thị của sản phẩm (máy/phụ kiện).</summary>
public enum ProductStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2
}

/// <summary>Vòng đời của một đơn hàng.</summary>
public enum OrderStatus
{
    Pending = 0,     // Chờ xác nhận
    Confirmed = 1,   // Đã xác nhận
    Shipping = 2,    // Đang giao
    Completed = 3,   // Hoàn tất
    Cancelled = 4    // Đã hủy
}

/// <summary>Phương thức thanh toán. Điện thoại giá cao nên có thêm trả góp.</summary>
public enum PaymentMethod
{
    Cod = 0,          // Thanh toán khi nhận hàng
    VnPay = 1,        // Cổng VNPay
    Installment = 2   // Trả góp qua công ty tài chính (mock)
}

/// <summary>Trạng thái một kỳ trả góp hàng tháng.</summary>
public enum InstallmentStatus
{
    Pending = 0,   // Chờ trả (chưa đến hạn, hoặc đến hạn nhưng chưa trả)
    Paid = 1,      // Đã trả
    Overdue = 2    // Quá hạn (thường tính lúc đọc: chưa trả & quá ngày đến hạn)
}

/// <summary>Trạng thái giao dịch thanh toán.</summary>
public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

/// <summary>Loại giảm giá của coupon.</summary>
public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

/// <summary>Mục đích của token dùng một lần.</summary>
public enum UserTokenPurpose
{
    EmailConfirm = 0,
    PasswordReset = 1
}

/// <summary>Trạng thái phiếu bảo hành gắn theo IMEI khi giao máy.</summary>
public enum WarrantyStatus
{
    Active = 0,     // Còn bảo hành
    Expired = 1,    // Hết hạn
    Void = 2        // Mất hiệu lực (đơn bị hủy/trả)
}

/// <summary>Trạng thái yêu cầu thu cũ đổi mới.</summary>
public enum TradeInStatus
{
    Pending = 0,    // Chờ định giá
    Quoted = 1,     // Đã báo giá
    Accepted = 2,   // Khách đồng ý
    Rejected = 3    // Từ chối/hủy
}

/// <summary>Loại thông báo realtime.</summary>
public enum NotificationType
{
    Order = 0,
    Promotion = 1,
    System = 2,
    Warranty = 3
}
