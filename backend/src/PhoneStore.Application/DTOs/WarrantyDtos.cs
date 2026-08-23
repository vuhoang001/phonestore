using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Application.DTOs;

/// <summary>Phiếu bảo hành gắn theo IMEI của một máy đã bán.</summary>
public record WarrantyDto(int Id, string Imei, string ProductName, string OrderCode,
    DateTime StartDate, DateTime EndDate, string Status);

/// <summary>Kết quả tra cứu bảo hành theo IMEI hoặc mã đơn (cho phép ẩn danh).</summary>
public record WarrantyLookupResultDto(bool Found, IReadOnlyList<WarrantyDto> Items);

/// <summary>Admin gán IMEI cho một dòng hàng (OrderItem) khi giao máy.</summary>
public record AssignImeiDto
{
    [Required] public int OrderItemId { get; init; }
    [Required] public string Imei { get; init; } = default!;
}
