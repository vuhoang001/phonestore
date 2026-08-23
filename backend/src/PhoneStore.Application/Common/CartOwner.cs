namespace PhoneStore.Application.Common;

/// <summary>Chủ sở hữu giỏ hàng: user đã đăng nhập (UserId) hoặc khách (GuestToken).</summary>
public readonly record struct CartOwner(int? UserId, string? GuestToken)
{
    public bool IsGuest => UserId is null;

    public static CartOwner ForUser(int userId) => new(userId, null);
    public static CartOwner ForGuest(string guestToken) => new(null, guestToken);
}
