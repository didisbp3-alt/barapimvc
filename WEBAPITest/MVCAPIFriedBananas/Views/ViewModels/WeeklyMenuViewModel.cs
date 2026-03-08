using DTO_MVCAPIContracts.Contracts;
using MVCAPIFriedBananas.Views.ViewModels;
using System;
using System.Collections.Generic;

namespace MVCAPIFriedBananas.ViewModels
{
    public class WeeklyMenuViewModel
    {
        public DateOnly WeekStart { get; set; }
        public List<MenuDayViewModel> Days { get; set; } = new();
        public HashSet<DateOnly> BookedDates { get; set; } = new();
    }
}