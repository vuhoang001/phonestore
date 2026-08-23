using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PhoneStore.API.Hubs;

/// <summary>
/// Hub thông báo realtime. Chỉ dùng để SERVER đẩy xuống client (server → client),
/// nên hub không cần method public nào. Client chỉ kết nối và lắng nghe sự kiện "notification".
/// SignalR tự nhóm các kết nối theo user (dựa trên claim NameIdentifier = userId trong JWT),
/// nên <c>Clients.User(userId)</c> gửi đúng tới mọi thiết bị của user đó.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
}
