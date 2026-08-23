using PhoneStore.Application.DTOs;

namespace PhoneStore.Application.Interfaces;

/// <summary>
/// Đẩy thông báo realtime tới client (qua WebSocket/SignalR).
/// Trừu tượng hoá ở tầng Application để không phụ thuộc trực tiếp SignalR (Clean Architecture);
/// hiện thực nằm ở tầng API.
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>Đẩy 1 thông báo tới tất cả kết nối đang mở của user (nếu online).</summary>
    Task PushAsync(int userId, NotificationDto notification);
}
