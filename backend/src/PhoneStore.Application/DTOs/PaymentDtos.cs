namespace PhoneStore.Application.DTOs;

/// <summary>Kết quả tạo phiên thanh toán — client dùng paymentUrl để chuyển hướng sang VNPAY.</summary>
public record CreatePaymentDto
{
    public string PaymentUrl { get; init; } = default!;
}

/// <summary>Kết quả sau khi verify callback từ VNPAY, dùng để redirect khách về frontend.</summary>
public record PaymentResultDto
{
    public bool Success { get; init; }
    public int? OrderId { get; init; }
    public string OrderCode { get; init; } = default!;
    public string ResponseCode { get; init; } = default!;
    public string Message { get; init; } = default!;
    /// <summary>URL trang kết quả bên frontend đã đính kèm tham số để redirect.</summary>
    public string RedirectUrl { get; init; } = default!;
}
