namespace MVCAPIFriedBananas.Models
{
    public class UserAdminDto
    {
        public int UId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public int? Role { get; set; }
        public decimal? Balance { get; set; }
    }
}
