using System.Collections.Generic;

namespace DTO_MVCAPIContracts.Contracts;

public sealed record OrderDto
{
    public int? OrderId { get; init; }
    public decimal Subtotal { get; set; }
    public decimal? Total { get; init; }
    public string? Status { get; init; }
    public DateTime? CreatedAt { get; init; }
    public IReadOnlyList<OrderItemDto>? Items { get; init; }
    public string? Notes { get; init; }
}