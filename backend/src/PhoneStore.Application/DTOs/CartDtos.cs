using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Application.DTOs;

public record CartDto
{
    public int Id { get; init; }
    public List<CartItemDto> Items { get; init; } = new();
    public decimal SubTotal { get; init; }
    public int TotalQuantity { get; init; }
}

public record CartItemDto
{
    public int Id { get; init; }
    public int VariantId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string? VariantInfo { get; init; }
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public int StockQuantity { get; init; }
    public decimal LineTotal { get; init; }
}

public record AddToCartDto
{
    [Required] public int VariantId { get; init; }
    [Range(1, int.MaxValue)] public int Quantity { get; init; } = 1;
}

public record UpdateCartItemDto
{
    [Range(0, int.MaxValue)] public int Quantity { get; init; }
}
