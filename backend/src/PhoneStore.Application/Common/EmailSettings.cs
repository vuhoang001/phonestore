namespace PhoneStore.Application.Common;

/// <summary>Cấu hình gửi email. Provider=Log ghi ra console (demo), Smtp gửi thật.</summary>
public class EmailSettings
{
    /// <summary>"Log" (mặc định, ghi console) hoặc "Smtp".</summary>
    public string Provider { get; set; } = "Log";
    public string FromName { get; set; } = "PhoneStore";
    public string FromEmail { get; set; } = "no-reply@phonestore.local";
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    /// <summary>URL frontend để dựng link trong email (xác nhận, đặt lại mật khẩu...).</summary>
    public string AppBaseUrl { get; set; } = "http://localhost:5173";
}

/// <summary>Cấu hình chính sách xác thực.</summary>
public class AuthSettings
{
    /// <summary>Bắt buộc xác nhận email trước khi đăng nhập (mặc định false cho demo).</summary>
    public bool RequireEmailConfirmation { get; set; }
}
