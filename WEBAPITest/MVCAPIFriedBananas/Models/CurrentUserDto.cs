namespace MVCAPIFriedBananas.Models
{
    public class CurrentUserDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public int? Role { get; set; }
        public decimal? Balance { get; set; }
    }
}