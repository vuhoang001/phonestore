using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Application.DTOs;

/// <summary>Yêu cầu thu cũ đổi mới.</summary>
public record TradeInDto(int Id, string OldDeviceModel, string Condition, string? Note,
    decimal QuotedPrice, string Status, int? TargetProductId, DateTime CreatedAt);

/// <summary>Khách tạo yêu cầu thu cũ đổi mới.</summary>
public record CreateTradeInDto
{
    [Required] public string OldDeviceModel { get; init; } = default!;
    [Required] public string Condition { get; init; } = default!;
    public string? Note { get; init; }
    public int? TargetProductId { get; init; }
}

/// <summary>Admin báo giá thu cho một yêu cầu.</summary>
public record QuoteTradeInDto
{
    [Range(0, double.MaxValue)] public decimal QuotedPrice { get; init; }
}
