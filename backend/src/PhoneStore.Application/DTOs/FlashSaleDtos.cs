using System.ComponentModel.DataAnnotations;
namespace PhoneStore.Application.DTOs;

public record FlashSaleDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public bool IsActive { get; init; }
    public bool IsRunning { get; init; }
    public List<FlashSaleItemDto> Items { get; init; } = new();
}

public record FlashSaleItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string ProductSlug { get; init; } = default!;
    public string? ProductImage { get; init; }
    public decimal OriginalPrice { get; init; }
    public decimal FlashPrice { get; init; }
    public int DiscountPercent { get; init; }
    public int QuantityLimit { get; init; }
    public int SoldCount { get; init; }
}

public record CreateFlashSaleDto
{
    [Required] public string Name { get; init; } = default!;
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public bool IsActive { get; init; } = true;
    public List<CreateFlashSaleItemDto> Items { get; init; } = new();
}

public record CreateFlashSaleItemDto
{
    public int ProductId { get; init; }
    [Range(0, double.MaxValue)] public decimal FlashPrice { get; init; }
    [Range(0, int.MaxValue)] public int QuantityLimit { get; init; }
}
