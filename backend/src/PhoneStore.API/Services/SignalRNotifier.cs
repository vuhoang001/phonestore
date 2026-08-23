using PhoneStore.API.Hubs;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace PhoneStore.API.Services;

/// <summary>Hiện thực <see cref="IRealtimeNotifier"/> bằng SignalR.</summary>
public class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;
    public SignalRNotifier(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task PushAsync(int userId, NotificationDto notification)
        // Clients.User dùng userId (khớp claim NameIdentifier trong JWT) → tới đúng mọi kết nối của user.
        => _hub.Clients.User(userId.ToString()).SendAsync("notification", notification);
}
