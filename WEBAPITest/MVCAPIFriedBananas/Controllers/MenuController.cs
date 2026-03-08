using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Services;
using MVCAPIFriedBananas.ViewModels;
using MVCAPIFriedBananas.Views.ViewModels;
using DTO_MVCAPIContracts.Contracts;

namespace MVCAPIFriedBananas.Controllers
{
    public class MenuController : Controller
    {
        private readonly MenuApiClient _menus;

        public MenuController(MenuApiClient menus)
        {
            _menus = menus;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateOnly? weekStart)
        {
            // Align to Monday
            var today = DateOnly.FromDateTime(DateTime.Today);
            var start = weekStart ?? today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var end = start.AddDays(6);

            var dtoList = await _menus.GetRangeAsync(start, end);

            var vm = new WeeklyMenuViewModel
            {
                WeekStart = start,
                Days = Enumerable.Range(0, 7)
                    .Select(i => BuildDay(start.AddDays(i), dtoList))
                    .ToList()
            };

            return View(vm);
        }

        private static MenuDayViewModel BuildDay(DateOnly day, IEnumerable<MenusDto> dtoList)
        {
            var daily = dtoList
                .Where(m => m.Date.HasValue && DateOnly.FromDateTime(m.Date.Value) == day)
                .ToList();

            return new MenuDayViewModel
            {
                Date = day,
                Normal = MapItem(daily.FirstOrDefault(m => m.Type == false)),
                Vegetarian = MapItem(daily.FirstOrDefault(m => m.Type == true))
            };
        }

        private static MenuItemViewModel? MapItem(MenusDto? m) =>
            m == null ? null : new MenuItemViewModel
            {
                MId = m.MId, // or m.Id if that’s your DTO property name
                Date = m.Date.HasValue ? DateOnly.FromDateTime(m.Date.Value) : default,
                Type = m.Type,
                MainDish = m.MainDish,
                Soup = m.Soup,
                Dessert = m.Dessert,
                Notes = m.Notes,
                MaxSeats = m.MaxSeats,
                UsedSeats = m.UsedSeats,
                AvailableSeats = m.AvailableSeats
            };
    }
    }