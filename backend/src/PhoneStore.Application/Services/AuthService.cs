using System.Security.Cryptography;
using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace PhoneStore.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IEmailSender _email;
    private readonly JwtSettings _jwtCfg;
    private readonly EmailSettings _emailCfg;
    private readonly AuthSettings _authCfg;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, IJwtTokenGenerator jwt,
        IEmailSender email, IOptions<JwtSettings> jwtCfg, IOptions<EmailSettings> emailCfg, IOptions<AuthSettings> authCfg)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _email = email;
        _jwtCfg = jwtCfg.Value;
        _emailCfg = emailCfg.Value;
        _authCfg = authCfg.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw AppException.Conflict("Email đã được sử dụng.");

        var user = new User
        {
            Email = email,
            PasswordHash = _hasher.Hash(dto.Password),
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone,
            Role = UserRole.Customer,
            EmailConfirmed = false
        };
        _db.Users.Add(user);
        _db.Carts.Add(new Cart { User = user });
        await _db.SaveChangesAsync();

        // Email chào mừng + xác nhận (best-effort: lỗi gửi mail KHÔNG chặn đăng ký).
        try
        {
            var confirmRaw = await CreateUserTokenAsync(user.Id, UserTokenPurpose.EmailConfirm, TimeSpan.FromDays(2));
            var confirmLink = $"{_emailCfg.AppBaseUrl}/confirm-email?token={confirmRaw}";
            await _email.SendAsync(user.Email, "Chào mừng đến PhoneStore 🎉", BuildWelcomeEmail(user.FullName, confirmLink));
        }
        catch { /* nuốt lỗi email — tài khoản vẫn được tạo */ }

        return await BuildResponseAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw AppException.Unauthorized("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            throw AppException.Forbidden("Tài khoản đã bị khóa.");
        if (!_hasher.Verify(dto.Password, user.PasswordHash))
            throw AppException.Unauthorized("Email hoặc mật khẩu không đúng.");
        if (_authCfg.RequireEmailConfirmation && !user.EmailConfirmed)
            throw AppException.Forbidden("Vui lòng xác nhận email trước khi đăng nhập.");

        return await BuildResponseAsync(user);
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var stored = await _db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash)
            ?? throw AppException.Unauthorized("Refresh token không hợp lệ.");
        if (!stored.IsActive)
            throw AppException.Unauthorized("Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.");

        // Xoay vòng: thu hồi token cũ, cấp token mới.
        stored.RevokedAt = DateTime.UtcNow;
        var response = await BuildResponseAsync(stored.User);
        return response;
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = Hash(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored != null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ConfirmEmailAsync(string token)
    {
        var stored = await FindUsableTokenAsync(token, UserTokenPurpose.EmailConfirm)
            ?? throw new AppException("Link xác nhận không hợp lệ hoặc đã hết hạn.");
        stored.UsedAt = DateTime.UtcNow;
        stored.User.EmailConfirmed = true;
        await _db.SaveChangesAsync();
    }

    public async Task ForgotPasswordAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        // Không tiết lộ email có tồn tại hay không.
        if (user != null)
        {
            var raw = await CreateUserTokenAsync(user.Id, UserTokenPurpose.PasswordReset, TimeSpan.FromHours(1));
            await _email.SendAsync(user.Email, "Đặt lại mật khẩu PhoneStore",
                $"Nhấn vào link để đặt lại mật khẩu (hết hạn sau 1 giờ): " +
                $"<a href=\"{_emailCfg.AppBaseUrl}/reset-password?token={raw}\">Đặt lại mật khẩu</a>");
        }
    }

    public async Task ResetPasswordAsync(string token, string newPassword)
    {
        var stored = await FindUsableTokenAsync(token, UserTokenPurpose.PasswordReset)
            ?? throw new AppException("Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        stored.UsedAt = DateTime.UtcNow;
        stored.User.PasswordHash = _hasher.Hash(newPassword);

        // Thu hồi mọi refresh token cũ để buộc đăng nhập lại.
        var tokens = await _db.RefreshTokens.Where(t => t.UserId == stored.UserId && t.RevokedAt == null).ToListAsync();
        foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<UserDto> GetProfileAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw AppException.NotFound("Không tìm thấy người dùng.");
        return ToDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw AppException.NotFound("Không tìm thấy người dùng.");
        user.FullName = dto.FullName.Trim();
        user.Phone = dto.Phone;
        user.AvatarUrl = dto.AvatarUrl;
        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw AppException.NotFound("Không tìm thấy người dùng.");
        if (!_hasher.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new AppException("Mật khẩu hiện tại không đúng.");
        user.PasswordHash = _hasher.Hash(dto.NewPassword);
        await _db.SaveChangesAsync();
    }

    // ---------- helpers ----------
    private async Task<AuthResponseDto> BuildResponseAsync(User user)
    {
        var (token, expiresAt) = _jwt.Generate(user);
        var rawRefresh = GenerateToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtCfg.RefreshTokenDays)
        });
        await _db.SaveChangesAsync();
        return new AuthResponseDto { Token = token, RefreshToken = rawRefresh, ExpiresAt = expiresAt, User = ToDto(user) };
    }

    private async Task<string> CreateUserTokenAsync(int userId, UserTokenPurpose purpose, TimeSpan lifetime)
    {
        var raw = GenerateToken();
        _db.UserTokens.Add(new UserToken
        {
            UserId = userId,
            Purpose = purpose,
            TokenHash = Hash(raw),
            ExpiresAt = DateTime.UtcNow.Add(lifetime)
        });
        await _db.SaveChangesAsync();
        return raw;
    }

    private async Task<UserToken?> FindUsableTokenAsync(string rawToken, UserTokenPurpose purpose)
    {
        var hash = Hash(rawToken);
        var stored = await _db.UserTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.Purpose == purpose);
        return stored is { IsUsable: true } ? stored : null;
    }

    private string BuildWelcomeEmail(string name, string confirmLink) => $@"
<div style='font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#242424'>
  <div style='background:linear-gradient(120deg,#1e6fff,#4d94ff);color:#fff;padding:28px 24px;border-radius:10px 10px 0 0;text-align:center'>
    <h2 style='margin:0'>📱 Chào mừng đến PhoneStore!</h2>
  </div>
  <div style='border:1px solid #eee;border-top:none;padding:24px;border-radius:0 0 10px 10px'>
    <p>Xin chào <b>{name}</b>,</p>
    <p>Tài khoản của bạn đã được tạo thành công. Cảm ơn bạn đã tham gia PhoneStore — điện thoại chính hãng, giá tốt mỗi ngày!</p>
    <p>Xác nhận email để bảo vệ tài khoản của bạn:</p>
    <div style='text-align:center;margin:24px 0'>
      <a href='{confirmLink}' style='background:#1e6fff;color:#fff;text-decoration:none;padding:12px 28px;border-radius:8px;font-weight:700;display:inline-block'>Xác nhận email</a>
    </div>
    <p style='color:#555'>Gợi ý: nhập mã <b>WELCOME10</b> khi thanh toán để được giảm 10% cho đơn đầu tiên 🎁</p>
    <p style='color:#999;font-size:12px'>Email tự động từ PhoneStore — đồ án môn học.</p>
  </div>
</div>";

    private static string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        Phone = u.Phone,
        AvatarUrl = u.AvatarUrl,
        Role = u.Role.ToString(),
        EmailConfirmed = u.EmailConfirmed
    };
}
