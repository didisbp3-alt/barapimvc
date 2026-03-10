namespace DTO_MVCAPIContracts.Contracts;

public sealed record LunchBookingDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public int MenuId { get; init; }
    public DateTime Date { get; init; }
    public DateTime CreatedAt { get; init; }
}