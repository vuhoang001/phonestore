using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace PhoneStore.Application.Services;

/// <summary>Tích hợp cổng thanh toán VNPAY (môi trường sandbox).</summary>
public class PaymentService : IPaymentService
{
    private readonly IAppDbContext _db;
    private readonly VnPaySettings _cfg;
    private readonly INotificationService _notif;

    public PaymentService(IAppDbContext db, IOptions<VnPaySettings> cfg, INotificationService notif)
    {
        _db = db;
        _cfg = cfg.Value;
        _notif = notif;
    }

    public async Task<CreatePaymentDto> CreateVnPayUrlAsync(int userId, int orderId, string ipAddress)
    {
        var order = await _db.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");
        if (order.UserId != userId)
            throw AppException.Forbidden("Bạn không có quyền thanh toán đơn hàng này.");
        if (order.Payment is null)
            throw new AppException("Đơn hàng chưa có thông tin thanh toán.");
        if (order.Payment.Status == PaymentStatus.Paid)
            throw new AppException("Đơn hàng này đã được thanh toán.");
        if (order.Status == OrderStatus.Cancelled)
            throw new AppException("Đơn hàng đã bị hủy, không thể thanh toán.");

        // Chưa có credential thật → trả URL trang giả lập để demo được toàn bộ luồng.
        if (_cfg.IsMock)
        {
            var sep = _cfg.MockUrl.Contains('?') ? '&' : '?';
            var mockUrl = $"{_cfg.MockUrl}{sep}orderId={order.Id}" +
                          $"&orderCode={Uri.EscapeDataString(order.OrderCode)}&amount={(long)order.Payment.Amount}";
            return new CreatePaymentDto { PaymentUrl = mockUrl };
        }

        // Giờ Việt Nam (GMT+7) — VNPAY yêu cầu định dạng yyyyMMddHHmmss.
        var now = DateTime.UtcNow.AddHours(7);
        var vnp = new VnPayLibrary();
        vnp.AddRequestData("vnp_Version", _cfg.Version);
        vnp.AddRequestData("vnp_Command", "pay");
        vnp.AddRequestData("vnp_TmnCode", _cfg.TmnCode);
        // VNPAY tính theo đơn vị nhỏ nhất → nhân 100.
        vnp.AddRequestData("vnp_Amount", ((long)(order.Payment.Amount * 100)).ToString());
        vnp.AddRequestData("vnp_CurrCode", "VND");
        vnp.AddRequestData("vnp_TxnRef", order.Id.ToString());
        vnp.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.OrderCode}");
        vnp.AddRequestData("vnp_OrderType", "other");
        vnp.AddRequestData("vnp_Locale", _cfg.Locale);
        vnp.AddRequestData("vnp_ReturnUrl", _cfg.ReturnUrl);
        vnp.AddRequestData("vnp_IpAddr", string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress);
        vnp.AddRequestData("vnp_CreateDate", now.ToString("yyyyMMddHHmmss"));
        vnp.AddRequestData("vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

        return new CreatePaymentDto { PaymentUrl = vnp.CreateRequestUrl(_cfg.BaseUrl, _cfg.HashSecret) };
    }

    public async Task<PaymentResultDto> HandleVnPayReturnAsync(IReadOnlyDictionary<string, string> query)
    {
        var vnp = new VnPayLibrary();
        foreach (var (key, value) in query)
            if (key.StartsWith("vnp_")) vnp.AddResponseData(key, value);

        var secureHash = query.TryGetValue("vnp_SecureHash", out var h) ? h : string.Empty;
        var responseCode = vnp.GetResponseData("vnp_ResponseCode");
        var transactionStatus = vnp.GetResponseData("vnp_TransactionStatus");
        var txnRef = vnp.GetResponseData("vnp_TxnRef");
        var transactionNo = vnp.GetResponseData("vnp_TransactionNo");

        // 1) Xác thực chữ ký chống giả mạo.
        if (!vnp.ValidateSignature(secureHash, _cfg.HashSecret))
            return Fail(txnRef, "97", "Chữ ký không hợp lệ.");

        if (!int.TryParse(txnRef, out var orderId))
            return Fail(txnRef, "01", "Không xác định được đơn hàng.");

        var order = await _db.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order?.Payment is null)
            return Fail(txnRef, "01", "Không tìm thấy đơn hàng.");

        // 2) Idempotent — nếu đã ghi nhận thanh toán thì trả lại kết quả thành công.
        if (order.Payment.Status == PaymentStatus.Paid)
            return Ok(order, "Đơn hàng đã được thanh toán.");

        // 3) Đối chiếu số tiền để tránh sai lệch.
        var expected = (long)(order.Payment.Amount * 100);
        if (!long.TryParse(vnp.GetResponseData("vnp_Amount"), out var paidAmount) || paidAmount != expected)
        {
            await MarkFailedAsync(order);
            return Fail(order.OrderCode, "04", "Số tiền thanh toán không khớp.");
        }

        // 4) "00" ở cả hai field mới coi là giao dịch thành công.
        if (responseCode == "00" && transactionStatus == "00")
        {
            await ApplySuccessAsync(order, transactionNo);
            return Ok(order, "Thanh toán thành công.");
        }

        await MarkFailedAsync(order);
        return Fail(order.OrderCode, responseCode, "Giao dịch không thành công hoặc bị hủy.");
    }

    public async Task<PaymentResultDto> CompleteMockAsync(int userId, int orderId, bool success)
    {
        if (!_cfg.IsMock)
            throw new AppException("Chế độ giả lập đã tắt (đang dùng VNPAY thật).");

        var order = await _db.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");
        if (order.UserId != userId)
            throw AppException.Forbidden("Bạn không có quyền thanh toán đơn hàng này.");
        if (order.Payment is null)
            throw new AppException("Đơn hàng chưa có thông tin thanh toán.");
        if (order.Payment.Status == PaymentStatus.Paid)
            return Ok(order, "Đơn hàng đã được thanh toán.");

        if (success)
        {
            await ApplySuccessAsync(order, $"MOCK{DateTime.UtcNow:yyMMddHHmmss}");
            return Ok(order, "Thanh toán (giả lập) thành công.");
        }
        await MarkFailedAsync(order);
        return Fail(order.OrderCode, "24", "Bạn đã hủy giao dịch (giả lập).");
    }

    /// <summary>Ghi nhận thanh toán thành công: cập nhật payment + lịch sử + thông báo.</summary>
    private async Task ApplySuccessAsync(Order order, string transactionNo)
    {
        order.Payment!.Status = PaymentStatus.Paid;
        order.Payment.PaidAt = DateTime.UtcNow;
        order.Payment.TransactionRef = transactionNo;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.Status,
            Note = $"Thanh toán VNPAY thành công (mã GD: {transactionNo})."
        });
        await _db.SaveChangesAsync();

        // Thông báo (ngoài giao dịch cập nhật payment): báo khách + báo admin → đẩy realtime.
        var amount = order.Payment.Amount;
        await _notif.NotifyUserAsync(order.UserId, "Thanh toán thành công",
            $"Đơn {order.OrderCode} đã thanh toán {amount:N0}đ qua VNPAY.", "order", $"/orders/{order.Id}");
        await _notif.NotifyAdminsAsync("Đơn đã thanh toán",
            $"Đơn {order.OrderCode} đã được thanh toán {amount:N0}đ qua VNPAY.", "order", $"/admin/orders?open={order.Id}");
    }

    private async Task MarkFailedAsync(Order order)
    {
        if (order.Payment!.Status == PaymentStatus.Pending)
        {
            order.Payment.Status = PaymentStatus.Failed;
            await _db.SaveChangesAsync();
        }
    }

    private PaymentResultDto Ok(Order order, string message) => new()
    {
        Success = true,
        OrderId = order.Id,
        OrderCode = order.OrderCode,
        ResponseCode = "00",
        Message = message,
        RedirectUrl = BuildRedirect(true, order.Id, order.OrderCode, "00")
    };

    private PaymentResultDto Fail(string orderCodeOrRef, string code, string message) => new()
    {
        Success = false,
        OrderCode = orderCodeOrRef,
        ResponseCode = code,
        Message = message,
        RedirectUrl = BuildRedirect(false, null, orderCodeOrRef, code)
    };

    /// <summary>Ghép URL trang kết quả bên frontend kèm tham số để hiển thị.</summary>
    private string BuildRedirect(bool success, int? orderId, string orderCode, string code)
    {
        var sep = _cfg.FrontendReturnUrl.Contains('?') ? '&' : '?';
        var status = success ? "success" : "failed";
        var oid = orderId?.ToString() ?? string.Empty;
        return $"{_cfg.FrontendReturnUrl}{sep}status={status}&orderId={Uri.EscapeDataString(oid)}" +
               $"&orderCode={Uri.EscapeDataString(orderCode)}&code={Uri.EscapeDataString(code)}";
    }
}
