namespace DTO_MVCAPIContracts.Contracts;

public sealed record MenuDto
{
    public int MId { get; init; }
    public DateTime Date { get; init; }
    public bool? Type { get; init; } // false = Normal, true = Vegetariano 
    public string? MainDish { get; init; }
    public int AvailableSeats { get; init; }
    public int? MaxSeats { get; init; }
}