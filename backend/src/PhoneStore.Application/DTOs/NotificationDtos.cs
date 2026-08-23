namespace PhoneStore.Application.DTOs;

public record NotificationDto
{
    public int Id { get; init; }
    public string Title { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string? Link { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record NotificationListDto
{
    public List<NotificationDto> Items { get; init; } = new();
    public int UnreadCount { get; init; }
}
