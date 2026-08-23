using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace PhoneStore.Application.Services;

public class OrderService : IOrderService
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;
    private readonly IWarrantyService _warranty;
    private readonly IEmailSender _email;
    private readonly EmailSettings _emailCfg;
    public OrderService(IAppDbContext db, IAuditLogger audit, INotificationService notif,
        IWarrantyService warranty, IEmailSender email, IOptions<EmailSettings> emailCfg)
    {
        _db = db;
        _audit = audit;
        _notif = notif;
        _warranty = warranty;
        _email = email;
        _emailCfg = emailCfg.Value;
    }

    // Các kỳ hạn trả góp hợp lệ (mock, lãi 0% demo — chia đều).
    private static readonly int[] InstallmentTerms = { 6, 9, 12 };

    public async Task<OrderDto> CreateAsync(int userId, CreateOrderDto dto)
    {
        var cart = await _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || cart.Items.Count == 0)
            throw new AppException("Giỏ hàng đang trống.");

        // Chỉ đặt các dòng được chọn (nếu client gửi danh sách), ngược lại đặt cả giỏ.
        var selectedItems = dto.CartItemIds.Count > 0
            ? cart.Items.Where(i => dto.CartItemIds.Contains(i.Id)).ToList()
            : cart.Items.ToList();
        if (selectedItems.Count == 0)
            throw new AppException("Chưa chọn sản phẩm nào để đặt hàng.");

        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == dto.AddressId && a.UserId == userId)
            ?? throw AppException.NotFound("Không tìm thấy địa chỉ giao hàng.");

        // Giá Flash Sale (nếu sản phẩm đang trong chương trình ĐANG CHẠY) — tính SERVER-SIDE,
        // không tin giá client. Lấy giá thấp nhất nếu 1 sản phẩm nằm ở nhiều chương trình.
        var nowUtc = DateTime.UtcNow;
        var productIds = selectedItems.Select(ci => ci.Variant.ProductId).Distinct().ToList();
        var flashPrices = await _db.FlashSaleItems.AsNoTracking()
            .Where(fi => productIds.Contains(fi.ProductId)
                && fi.FlashSale.IsActive && fi.FlashSale.StartAt <= nowUtc && fi.FlashSale.EndAt > nowUtc)
            .GroupBy(fi => fi.ProductId)
            .Select(g => new { ProductId = g.Key, Price = g.Min(x => x.FlashPrice) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Price);

        // Kiểm tra tồn kho & tính tiền
        decimal subTotal = 0;
        var orderItems = new List<OrderItem>();
        foreach (var ci in selectedItems)
        {
            var variant = ci.Variant;
            if (ci.Quantity > variant.StockQuantity)
                throw new AppException($"Sản phẩm '{variant.Product.Name}' chỉ còn {variant.StockQuantity} trong kho.");

            variant.StockQuantity -= ci.Quantity; // trừ tồn kho
            variant.Product.SoldCount += ci.Quantity; // tăng lượt đã bán
            var variantInfo = string.Join(" / ", new[] { variant.Color, variant.Storage }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            // Đơn giá thực thu: giá flash nếu đang sale và rẻ hơn giá gốc, ngược lại giá variant.
            var unitPrice = flashPrices.TryGetValue(variant.ProductId, out var fp) && fp < variant.Price
                ? fp : variant.Price;
            subTotal += unitPrice * ci.Quantity;
            orderItems.Add(new OrderItem
            {
                VariantId = variant.Id,
                ProductNameSnapshot = variant.Product.Name,
                VariantInfoSnapshot = string.IsNullOrEmpty(variantInfo) ? null : variantInfo,
                PriceSnapshot = unitPrice,
                Quantity = ci.Quantity
            });
        }

        // Phí ship — bắt buộc phương thức phải tồn tại & đang bật nếu client có gửi.
        decimal shippingFee = 0;
        if (dto.ShippingMethodId is int smId)
        {
            var sm = await _db.ShippingMethods.FirstOrDefaultAsync(s => s.Id == smId && s.IsActive)
                ?? throw new AppException("Phương thức vận chuyển không hợp lệ.");
            shippingFee = sm.BaseFee;
        }

        // Áp coupon
        decimal discount = 0;
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(dto.CouponCode))
        {
            coupon = await ValidateCoupon(dto.CouponCode.Trim(), subTotal, userId);
            discount = coupon.DiscountType == DiscountType.Percentage
                ? Math.Round(subTotal * coupon.DiscountValue / 100m, 0)
                : coupon.DiscountValue;
            discount = Math.Min(discount, subTotal);
        }

        var total = subTotal - discount + shippingFee;

        var method = Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var pm) ? pm : PaymentMethod.Cod;

        // Trả góp (đặc thù điện thoại): chỉ áp khi client chọn số kỳ hợp lệ. Lãi 0% demo → chia đều.
        int? installmentMonths = null;
        decimal? installmentMonthly = null;
        if (dto.InstallmentMonths is int months && months > 0)
        {
            if (!InstallmentTerms.Contains(months))
                throw new AppException("Kỳ hạn trả góp không hợp lệ (chỉ hỗ trợ 6/9/12 tháng).");
            installmentMonths = months;
            installmentMonthly = Math.Round(total / months, 0);
        }

        var order = new Order
        {
            UserId = userId,
            OrderCode = GenerateOrderCode(),
            SubTotal = subTotal,
            DiscountAmount = discount,
            ShippingFee = shippingFee,
            TotalAmount = total,
            Status = OrderStatus.Pending,
            AddressId = address.Id,
            ShippingAddressSnapshot = FormatAddress(address),
            Note = dto.Note,
            InstallmentMonths = installmentMonths,
            InstallmentMonthly = installmentMonthly,
            Items = orderItems
        };
        order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Pending, Note = "Đơn hàng được tạo." });

        order.Payment = new Payment
        {
            Method = method,
            Amount = total,
            Status = PaymentStatus.Pending
        };

        if (coupon != null)
        {
            coupon.UsedCount++;
            order.OrderCoupons.Add(new OrderCoupon { Coupon = coupon, UserId = userId });
        }

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(selectedItems); // chỉ xóa các dòng đã đặt

        // Gom toàn bộ thao tác ghi vào 1 transaction. Concurrency token (xmin) trên kho & coupon
        // đảm bảo: hai đơn song song không thể bán quá tồn hay vượt lượt mã — đơn "thua" sẽ rollback.
        // Đặt trong block riêng để transaction được dispose TRƯỚC khi gửi thông báo (tránh SaveChanges
        // của NotificationService chạy trên transaction đã commit).
        await using (var tx = await _db.BeginTransactionAsync())
        {
            try
            {
                await _db.SaveChangesAsync(); // trừ kho + tạo đơn (được kiểm tra concurrency tại đây)
                await tx.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                throw new AppException("Kho hàng hoặc mã giảm giá vừa thay đổi, vui lòng thử lại.");
            }
        }

        // Thông báo (ngoài transaction đơn hàng): báo khách + báo admin có đơn mới → đẩy realtime.
        await _notif.NotifyUserAsync(userId, "Đặt hàng thành công",
            $"Đơn {order.OrderCode} trị giá {total:N0}đ đã được tạo và đang chờ xác nhận.", "order", $"/orders/{order.Id}");
        await _notif.NotifyAdminsAsync("Đơn hàng mới",
            $"Khách vừa đặt đơn {order.OrderCode} trị giá {total:N0}đ.", "order", $"/admin/orders?open={order.Id}");

        // Email xác nhận đơn hàng (best-effort: lỗi gửi mail KHÔNG làm hỏng việc đặt hàng).
        try
        {
            var buyer = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId).Select(u => new { u.Email, u.FullName }).FirstOrDefaultAsync();
            if (buyer != null && !string.IsNullOrWhiteSpace(buyer.Email))
                await _email.SendAsync(buyer.Email, $"Xác nhận đơn hàng {order.OrderCode} · PhoneStore",
                    BuildOrderEmail(order, buyer.FullName));
        }
        catch { /* nuốt lỗi email — thông báo trong app vẫn là kênh chính */ }

        return await GetByIdAsync(userId, UserRole.Customer.ToString(), order.Id);
    }

    public async Task<PagedResult<OrderDto>> GetMyOrdersAsync(int userId, PaginationQuery query, string? status)
    {
        var q = _db.Orders.AsNoTracking().Where(o => o.UserId == userId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var st))
            q = q.Where(o => o.Status == st);
        return await Paginate(q.OrderByDescending(o => o.CreatedAt), query);
    }

    public async Task<PagedResult<OrderDto>> GetAllAsync(PaginationQuery query, string? status,
        DateTime? from = null, DateTime? to = null, string? keyword = null)
    {
        var q = _db.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var st))
            q = q.Where(o => o.Status == st);
        // Lọc theo khoảng ngày (chuẩn hoá UTC vì cột là timestamptz).
        if (from is DateTime f)
        {
            var fromUtc = DateTime.SpecifyKind(f.Date, DateTimeKind.Utc);
            q = q.Where(o => o.CreatedAt >= fromUtc);
        }
        if (to is DateTime t)
        {
            var toUtc = DateTime.SpecifyKind(t.Date, DateTimeKind.Utc).AddDays(1); // hết ngày "đến"
            q = q.Where(o => o.CreatedAt < toUtc);
        }
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            q = q.Where(o => o.OrderCode.ToLower().Contains(kw));
        }
        return await Paginate(q.OrderByDescending(o => o.CreatedAt), query);
    }

    public async Task<OrderDto> GetByIdAsync(int userId, string? role, int orderId)
    {
        var order = await LoadFull().FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

        var isAdmin = role is nameof(UserRole.Admin);
        if (!isAdmin && order.UserId != userId)
            throw AppException.Forbidden("Bạn không có quyền xem đơn hàng này.");

        return ToDto(order);
    }

    public async Task<OrderDto> UpdateStatusAsync(int orderId, UpdateOrderStatusDto dto)
    {
        var order = await LoadFull().FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

        if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var newStatus))
            throw new AppException("Trạng thái không hợp lệ.");

        // Quy trình chuẩn: chỉ cho phép chuyển theo các bước hợp lệ.
        var oldStatus = order.Status;
        if (newStatus == order.Status)
            throw new AppException($"Đơn hàng đã ở trạng thái '{StatusVi(order.Status)}'.");
        if (!AllowedTransitions[order.Status].Contains(newStatus))
            throw new AppException($"Không thể chuyển từ '{StatusVi(order.Status)}' sang '{StatusVi(newStatus)}'.");

        await _audit.LogAsync("OrderStatusChanged", "Order", order.Id, $"{oldStatus} → {newStatus}");

        if (newStatus == OrderStatus.Cancelled)
        {
            await RestockAsync(order);
            SettlePaymentOnCancel(order);
            order.CancelReason = string.IsNullOrWhiteSpace(dto.Note) ? "Admin hủy" : dto.Note.Trim();
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        order.StatusHistory.Add(new OrderStatusHistory { Status = newStatus, Note = dto.Note });

        if (newStatus == OrderStatus.Completed && order.Payment != null && order.Payment.Status == PaymentStatus.Pending)
        {
            order.Payment.Status = PaymentStatus.Paid;
            order.Payment.PaidAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        // Khi giao hàng (Shipping): gán IMEI cho từng máy & tạo phiếu bảo hành (đặc thù điện thoại).
        // Làm sau khi đã lưu trạng thái để WarrantyService tự lưu bản ghi của nó.
        if (newStatus == OrderStatus.Shipping && dto.ImeiAssignments.Count > 0)
        {
            foreach (var a in dto.ImeiAssignments)
                await _warranty.AssignImeiAsync(order.Id, a);
        }

        await NotifyStatusAsync(order, newStatus); // báo khách + đẩy realtime
        return await GetByIdAsync(order.UserId, UserRole.Admin.ToString(), order.Id);
    }

    public async Task<OrderDto> CancelByCustomerAsync(int userId, int orderId, string? reason = null)
    {
        var order = await LoadFull().FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");
        if (order.UserId != userId)
            throw AppException.Forbidden("Bạn không có quyền hủy đơn này.");
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new AppException("Chỉ có thể hủy đơn khi đang chờ xác nhận hoặc đã xác nhận.");

        await RestockAsync(order);
        order.Status = OrderStatus.Cancelled;
        order.CancelReason = string.IsNullOrWhiteSpace(reason) ? "Khách đổi ý" : reason.Trim();
        order.UpdatedAt = DateTime.UtcNow;
        order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Cancelled, Note = $"Khách hủy: {order.CancelReason}" });
        SettlePaymentOnCancel(order);

        await _db.SaveChangesAsync();
        await NotifyStatusAsync(order, OrderStatus.Cancelled); // báo khách
        await _notif.NotifyAdminsAsync("Khách hủy đơn",
            $"Khách đã hủy đơn {order.OrderCode}.", "order", $"/admin/orders?open={order.Id}"); // báo admin
        return ToDto(order);
    }

    /// <summary>Tự động hủy các đơn Pending quá hạn chưa thanh toán (gọi từ background job).</summary>
    public async Task<int> CancelExpiredAsync(TimeSpan olderThan)
    {
        var threshold = DateTime.UtcNow - olderThan;
        // Chỉ áp dụng cho đơn thanh toán ONLINE chưa trả tiền.
        // COD trả khi nhận hàng nên KHÔNG hết hạn vì lý do "chưa thanh toán".
        var expired = await LoadFull()
            .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < threshold
                        && o.Payment != null
                        && o.Payment.Method != PaymentMethod.Cod
                        && o.Payment.Status != PaymentStatus.Paid)
            .ToListAsync();

        foreach (var order in expired)
        {
            await RestockAsync(order);
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Cancelled, Note = "Tự động hủy do quá hạn thanh toán." });
            SettlePaymentOnCancel(order);
        }
        if (expired.Count > 0)
        {
            await _db.SaveChangesAsync();
            // Báo từng khách đơn bị tự hủy (sau khi đã lưu trạng thái) → đẩy realtime.
            foreach (var order in expired) await NotifyStatusAsync(order, OrderStatus.Cancelled);
        }
        return expired.Count;
    }

    /// <summary>Hoàn kho, giảm lượt đã bán và hoàn lượt coupon khi đơn bị hủy.</summary>
    private async Task RestockAsync(Order order)
    {
        foreach (var item in order.Items)
        {
            var variant = await _db.ProductVariants.Include(v => v.Product).FirstOrDefaultAsync(v => v.Id == item.VariantId);
            if (variant != null)
            {
                variant.StockQuantity += item.Quantity;
                variant.Product.SoldCount = Math.Max(0, variant.Product.SoldCount - item.Quantity);
            }
        }
        // Hoàn lại lượt sử dụng của các mã giảm giá đã áp cho đơn.
        foreach (var oc in order.OrderCoupons)
        {
            var coupon = await _db.Coupons.FindAsync(oc.CouponId);
            if (coupon != null) coupon.UsedCount = Math.Max(0, coupon.UsedCount - 1);
        }
    }

    /// <summary>Đơn đã trả tiền → đánh dấu hoàn tiền; chưa trả → đánh dấu thất bại.</summary>
    private static void SettlePaymentOnCancel(Order order)
    {
        if (order.Payment is null) return;
        order.Payment.Status = order.Payment.Status == PaymentStatus.Paid
            ? PaymentStatus.Refunded
            : PaymentStatus.Failed;
    }

    /// <summary>Quy trình trạng thái đơn hàng (state machine) kiểu production.</summary>
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        [OrderStatus.Confirmed] = new[] { OrderStatus.Shipping, OrderStatus.Cancelled },
        [OrderStatus.Shipping] = new[] { OrderStatus.Completed },
        [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    private static string StatusVi(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Completed => "Hoàn tất",
        OrderStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };

    private static readonly Dictionary<OrderStatus, string> StatusMessage = new()
    {
        [OrderStatus.Confirmed] = "đã được xác nhận và đang chuẩn bị hàng",
        [OrderStatus.Shipping] = "đang được giao đến bạn",
        [OrderStatus.Completed] = "đã giao thành công. Cảm ơn bạn!",
        [OrderStatus.Cancelled] = "đã bị hủy"
    };

    private async Task NotifyStatusAsync(Order order, OrderStatus status)
    {
        if (!StatusMessage.TryGetValue(status, out var msg)) return;
        await _notif.NotifyUserAsync(order.UserId, "Cập nhật đơn hàng",
            $"Đơn {order.OrderCode} {msg}.", "order", $"/orders/{order.Id}");

        // Email cập nhật trạng thái (best-effort).
        try
        {
            var buyer = await _db.Users.AsNoTracking()
                .Where(u => u.Id == order.UserId).Select(u => new { u.Email, u.FullName }).FirstOrDefaultAsync();
            if (buyer != null && !string.IsNullOrWhiteSpace(buyer.Email))
                await _email.SendAsync(buyer.Email, $"Đơn {order.OrderCode} · {StatusVi(status)} · PhoneStore",
                    BuildStatusEmail(order, status, buyer.FullName, msg));
        }
        catch { /* nuốt lỗi email */ }
    }

    private static readonly Dictionary<OrderStatus, (string emoji, string color)> StatusStyle = new()
    {
        [OrderStatus.Confirmed] = ("✅", "#3b82f6"),
        [OrderStatus.Shipping] = ("🚚", "#8b5cf6"),
        [OrderStatus.Completed] = ("🎉", "#22c55e"),
        [OrderStatus.Cancelled] = ("❌", "#ef4444")
    };

    private string BuildStatusEmail(Order order, OrderStatus status, string name, string msg)
    {
        var (emoji, color) = StatusStyle.TryGetValue(status, out var s) ? s : ("📦", "#1e6fff");
        var link = $"{_emailCfg.AppBaseUrl}/orders/{order.Id}";
        var reasonLine = status == OrderStatus.Cancelled && !string.IsNullOrWhiteSpace(order.CancelReason)
            ? $"<p style='color:#555'>Lý do: {order.CancelReason}</p>" : "";
        return $@"
<div style='font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#242424'>
  <div style='background:{color};color:#fff;padding:22px 24px;border-radius:10px 10px 0 0;text-align:center'>
    <h2 style='margin:0'>{emoji} {StatusVi(status)}</h2>
  </div>
  <div style='border:1px solid #eee;border-top:none;padding:24px;border-radius:0 0 10px 10px'>
    <p>Chào <b>{name}</b>,</p>
    <p>Đơn hàng <b>{order.OrderCode}</b> {msg}.</p>
    {reasonLine}
    <p style='color:#555'><b>Tổng tiền:</b> <span style='color:#1e6fff;font-weight:700'>{order.TotalAmount:N0}đ</span></p>
    <div style='text-align:center;margin:24px 0'>
      <a href='{link}' style='background:#1e6fff;color:#fff;text-decoration:none;padding:12px 28px;border-radius:8px;font-weight:700;display:inline-block'>Xem chi tiết đơn hàng</a>
    </div>
    <p style='color:#999;font-size:12px'>Email tự động từ PhoneStore — đồ án môn học.</p>
  </div>
</div>";
    }

    // ---------- helpers ----------
    private IQueryable<Order> LoadFull() => _db.Orders
        .Include(o => o.Items)
        .Include(o => o.StatusHistory)
        .Include(o => o.Payment)
        .Include(o => o.OrderCoupons);

    private async Task<PagedResult<OrderDto>> Paginate(IQueryable<Order> baseQuery, PaginationQuery query)
    {
        var withIncludes = baseQuery
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .Include(o => o.Payment);

        var total = await baseQuery.CountAsync();
        var items = await withIncludes
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<OrderDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = query.Page, PageSize = query.PageSize, TotalItems = total
        };
    }

    private async Task<Coupon> ValidateCoupon(string code, decimal orderAmount, int userId)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower())
            ?? throw AppException.NotFound("Mã giảm giá không tồn tại.");
        var now = DateTime.UtcNow;
        if (!coupon.IsActive || now < coupon.StartDate || now > coupon.EndDate)
            throw new AppException("Mã giảm giá đã hết hạn hoặc chưa có hiệu lực.");
        if (coupon.UsedCount >= coupon.UsageLimit)
            throw new AppException("Mã giảm giá đã hết lượt sử dụng.");
        if (orderAmount < coupon.MinOrderAmount)
            throw new AppException($"Đơn hàng tối thiểu {coupon.MinOrderAmount:N0}đ để dùng mã này.");
        // Mỗi người chỉ dùng mỗi mã một lần (không tính đơn đã hủy).
        var usedByUser = await _db.OrderCoupons
            .AnyAsync(oc => oc.CouponId == coupon.Id && oc.UserId == userId
                            && oc.Order.Status != OrderStatus.Cancelled);
        if (usedByUser)
            throw new AppException("Bạn đã sử dụng mã giảm giá này rồi.");
        return coupon;
    }

    private static string GenerateOrderCode() =>
        $"ORD{DateTime.UtcNow:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

    /// <summary>Dựng nội dung email HTML xác nhận đơn hàng.</summary>
    private string BuildOrderEmail(Order order, string customerName)
    {
        string Money(decimal v) => $"{v:N0}đ";
        var rows = string.Join("", order.Items.Select(i =>
            $"<tr><td style='padding:8px 0;border-bottom:1px solid #eee'>{i.ProductNameSnapshot}" +
            $"{(string.IsNullOrEmpty(i.VariantInfoSnapshot) ? "" : $" <span style='color:#888'>({i.VariantInfoSnapshot})</span>")}" +
            $"<br><span style='color:#888;font-size:13px'>{Money(i.PriceSnapshot)} × {i.Quantity}</span></td>" +
            $"<td style='padding:8px 0;border-bottom:1px solid #eee;text-align:right;white-space:nowrap'>{Money(i.PriceSnapshot * i.Quantity)}</td></tr>"));

        var installmentLine = order.InstallmentMonths is int m && order.InstallmentMonthly is decimal monthly
            ? $"<tr><td>Trả góp</td><td style='text-align:right'>{m} tháng × {Money(monthly)}</td></tr>" : "";

        var link = $"{_emailCfg.AppBaseUrl}/orders/{order.Id}";
        return $@"
<div style='font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#242424'>
  <div style='background:#1e6fff;color:#fff;padding:20px 24px;border-radius:10px 10px 0 0'>
    <h2 style='margin:0'>📱 PhoneStore</h2>
  </div>
  <div style='border:1px solid #eee;border-top:none;padding:24px;border-radius:0 0 10px 10px'>
    <p>Chào <b>{customerName}</b>,</p>
    <p>Cảm ơn bạn đã đặt hàng! Đơn <b>{order.OrderCode}</b> đã được tạo và đang <b>chờ xác nhận</b>.</p>
    <table style='width:100%;border-collapse:collapse;margin:16px 0'>{rows}</table>
    <table style='width:100%;font-size:14px'>
      <tr><td>Tạm tính</td><td style='text-align:right'>{Money(order.SubTotal)}</td></tr>
      {(order.DiscountAmount > 0 ? $"<tr><td>Giảm giá</td><td style='text-align:right'>-{Money(order.DiscountAmount)}</td></tr>" : "")}
      <tr><td>Phí vận chuyển</td><td style='text-align:right'>{Money(order.ShippingFee)}</td></tr>
      {installmentLine}
      <tr><td style='padding-top:8px;font-weight:700;font-size:16px'>Tổng cộng</td>
          <td style='padding-top:8px;text-align:right;font-weight:700;font-size:16px;color:#1e6fff'>{Money(order.TotalAmount)}</td></tr>
    </table>
    <p style='margin-top:8px;color:#555'><b>Giao đến:</b> {order.ShippingAddressSnapshot}</p>
    <div style='text-align:center;margin:24px 0'>
      <a href='{link}' style='background:#1e6fff;color:#fff;text-decoration:none;padding:12px 28px;border-radius:8px;font-weight:700;display:inline-block'>Xem chi tiết đơn hàng</a>
    </div>
    <p style='color:#999;font-size:12px'>Email tự động từ PhoneStore — đồ án môn học.</p>
  </div>
</div>";
    }

    private static string FormatAddress(Address a)
    {
        // Bỏ qua phần rỗng (vd: quận/huyện ở hệ hành chính 2 cấp) để không dư dấu phẩy
        var parts = new[] { a.Detail, a.Ward, a.District, a.Province }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var line = $"{a.RecipientName} | {a.Phone} | {string.Join(", ", parts)}";
        // Ghi chú giao hàng (nếu có) gắn cuối để shipper thấy khi xem đơn
        return string.IsNullOrWhiteSpace(a.Note) ? line : $"{line} | Ghi chú: {a.Note.Trim()}";
    }

    private static OrderDto ToDto(Order o) => new()
    {
        Id = o.Id,
        OrderCode = o.OrderCode,
        SubTotal = o.SubTotal,
        DiscountAmount = o.DiscountAmount,
        ShippingFee = o.ShippingFee,
        TotalAmount = o.TotalAmount,
        Status = o.Status.ToString(),
        ShippingAddress = o.ShippingAddressSnapshot,
        Note = o.Note,
        CreatedAt = o.CreatedAt,
        PaymentMethod = o.Payment?.Method.ToString(),
        PaymentStatus = o.Payment?.Status.ToString(),
        InstallmentMonths = o.InstallmentMonths,
        InstallmentMonthly = o.InstallmentMonthly,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            VariantId = i.VariantId,
            ProductName = i.ProductNameSnapshot,
            VariantInfo = i.VariantInfoSnapshot,
            Price = i.PriceSnapshot,
            Quantity = i.Quantity,
            LineTotal = i.PriceSnapshot * i.Quantity,
            Imei = i.Imei
        }).ToList(),
        StatusHistory = o.StatusHistory.OrderBy(h => h.CreatedAt).Select(h => new OrderStatusHistoryDto
        {
            Status = h.Status.ToString(), Note = h.Note, ChangedAt = h.CreatedAt
        }).ToList(),
        AllowedNextStatuses = AllowedTransitions[o.Status].Select(s => s.ToString()).ToList(),
        CanCancelByCustomer = o.Status is OrderStatus.Pending or OrderStatus.Confirmed
    };
}
