using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Application.DTOs;

public record RegisterDto
{
    [Required, EmailAddress] public string Email { get; init; } = default!;
    [Required, MinLength(6)] public string Password { get; init; } = default!;
    [Required] public string FullName { get; init; } = default!;
    public string? Phone { get; init; }
}

public record LoginDto
{
    [Required, EmailAddress] public string Email { get; init; } = default!;
    [Required] public string Password { get; init; } = default!;
}

public record AuthResponseDto
{
    public string Token { get; init; } = default!;
    public string RefreshToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = default!;
}

public record UserDto
{
    public int Id { get; init; }
    public string Email { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public string? Phone { get; init; }
    public string? AvatarUrl { get; init; }
    public string Role { get; init; } = default!;
    public bool EmailConfirmed { get; init; }
}

public record RefreshRequestDto
{
    [Required] public string RefreshToken { get; init; } = default!;
}

public record ForgotPasswordDto
{
    [Required, EmailAddress] public string Email { get; init; } = default!;
}

public record ResetPasswordDto
{
    [Required] public string Token { get; init; } = default!;
    [Required, MinLength(6)] public string NewPassword { get; init; } = default!;
}

public record UpdateProfileDto
{
    [Required] public string FullName { get; init; } = default!;
    public string? Phone { get; init; }
    public string? AvatarUrl { get; init; }
}

public record ChangePasswordDto
{
    [Required] public string CurrentPassword { get; init; } = default!;
    [Required, MinLength(6)] public string NewPassword { get; init; } = default!;
}
