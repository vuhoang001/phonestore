namespace PhoneStore.Application.Common;

/// <summary>Cấu hình JWT bind từ appsettings.</summary>
public class JwtSettings
{
    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    /// <summary>Thời hạn access token (phút). Ngắn để giảm rủi ro nếu bị lộ.</summary>
    public int ExpiryMinutes { get; set; } = 60;
    /// <summary>Thời hạn refresh token (ngày).</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
