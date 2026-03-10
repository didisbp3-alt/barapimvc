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
        private readonly BookingApiClient _bookings;

        public MenuController(MenuApiClient menus, BookingApiClient bookings)
        {
            _menus = menus;
            _bookings = bookings;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateOnly? weekStart)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var start = weekStart ?? today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);

            if (start.DayOfWeek == DayOfWeek.Saturday)
                start = start.AddDays(2);
            else if (start.DayOfWeek == DayOfWeek.Sunday)
                start = start.AddDays(1);

            var end = start.AddDays(4);
            var dtoList = await _menus.GetRangeAsync(start, end);

            var bookedDates = new HashSet<DateOnly>();
            if (User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    var myBookings = await _bookings.GetMyBookingsAsync();
                    foreach (var b in myBookings)
                    {
                        var d = DateOnly.FromDateTime(b.Date);
                        if (d >= start && d <= end)
                            bookedDates.Add(d);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[MenuController] Could not load bookings: {ex.Message}");
                }
            }

            var vm = new WeeklyMenuViewModel
            {
                WeekStart    = start,
                Days         = Enumerable.Range(0, 5).Select(i => BuildDay(start.AddDays(i), dtoList)).ToList(),
                BookedDates  = bookedDates
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
                Date        = day,
                Normal      = MapItem(daily.FirstOrDefault(m => m.Type == false)),
                Vegetarian  = MapItem(daily.FirstOrDefault(m => m.Type == true))
            };
        }

        private static MenuItemViewModel? MapItem(MenusDto? m) =>
            m == null ? null : new MenuItemViewModel
            {
                MId            = m.MId,
                Date           = m.Date.HasValue ? DateOnly.FromDateTime(m.Date.Value) : default,
                Type           = m.Type,
                MainDish       = m.MainDish,
                Soup           = m.Soup,
                Dessert        = m.Dessert,
                Notes          = m.Notes,
                MaxSeats       = m.MaxSeats,
                UsedSeats      = m.UsedSeats,
                AvailableSeats = m.AvailableSeats
            };
    }
}
