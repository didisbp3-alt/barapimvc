namespace MVCAPIFriedBananas.Views.ViewModels
{
    public class MenuItemViewModel
    {
        public int MId { get; set; }
        public DateOnly Date { get; set; }
        public bool? Type { get; set; }               // false = Normal, true = Veggie
        public string MainDish { get; set; } = "";
        public string Soup { get; set; } = "";
        public string Dessert { get; set; } = "";
        public string Notes { get; set; } = "";
        public int? MaxSeats { get; set; }
        public int? UsedSeats { get; set; }
        public int AvailableSeats { get; set; }
    }
}
