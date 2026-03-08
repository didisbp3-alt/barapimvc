namespace MVCAPIFriedBananas.Views.ViewModels
{
    public class MenuDayViewModel
    {
        public DateOnly Date { get; set; }
        public MenuItemViewModel? Normal { get; set; }
        public MenuItemViewModel? Vegetarian { get; set; }
    }
}
