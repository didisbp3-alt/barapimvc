namespace DTO_MVCAPIContracts.Contracts;

public sealed record OrderItemDto
{
    public int ProductId { get; init; }
    public string Name { get; set; } = "";

    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}