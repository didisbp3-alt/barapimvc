using System;
using System.Collections.Generic;
using DTO_MVCAPIContracts.Contracts;

namespace MVCAPIFriedBananas.ViewModels
{
    public sealed class DayMenu
    {
        public DateTime Date { get; set; }
        public MenusDto? Normal { get; set; }
        public MenusDto? Vegetarian { get; set; }
    }

    public sealed class WeeklyMenuViewModel
    {
        public DateTime WeekStart { get; set; }
        public List<DayMenu> Days { get; set; } = new();
    }
}