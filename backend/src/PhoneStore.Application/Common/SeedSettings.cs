namespace PhoneStore.Application.Common;

/// <summary>
/// Cấu hình khởi tạo dữ liệu nền tảng (bootstrap) — lấy từ appsettings/biến môi trường,
/// KHÔNG hardcode credential trong mã. Tạo tài khoản admin đầu tiên + (tùy chọn) dữ liệu demo.
/// </summary>
public class SeedSettings
{
    /// <summary>Tài khoản admin đầu tiên để đăng nhập và quản trị.</summary>
    public SeedAccount Admin { get; set; } = new();
    /// <summary>Có tạo tài khoản khách demo hay không.</summary>
    public bool SeedDemoCustomer { get; set; } = true;
    public SeedAccount Customer { get; set; } = new();
    /// <summary>Có nạp catalog điện thoại demo (từ seed-phones.json) hay không.</summary>
    public bool SeedProducts { get; set; } = true;
}

public class SeedAccount
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? Phone { get; set; }
}
