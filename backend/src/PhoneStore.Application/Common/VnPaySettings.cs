namespace PhoneStore.Application.Common;

/// <summary>Cấu hình cổng thanh toán VNPAY (sandbox) bind từ appsettings.</summary>
public class VnPaySettings
{
    public string TmnCode { get; set; } = default!;
    public string HashSecret { get; set; } = default!;
    public string BaseUrl { get; set; } = default!;
    public string ReturnUrl { get; set; } = default!;
    public string FrontendReturnUrl { get; set; } = default!;
    public string MockUrl { get; set; } = default!;
    public bool Mock { get; set; }
    public string Version { get; set; } = "2.1.0";
    public string Locale { get; set; } = "vn";

    /// <summary>Tự bật giả lập khi chưa cấu hình credential thật (còn placeholder).</summary>
    public bool IsMock => Mock || string.IsNullOrWhiteSpace(TmnCode) || TmnCode == "YOUR_TMNCODE";
}
