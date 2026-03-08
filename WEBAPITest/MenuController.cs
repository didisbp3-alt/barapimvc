using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Services;
using MVCAPIFriedBananas.ViewModels;
using DTO_MVCAPIContracts.Contracts;

namespace MVCAPIFriedBananas.Controllers;

public class MenuController : Controller
{
    #region Dependências
    private readonly MenuApiClient _menuApi;

    public MenuController(MenuApiClient menuApi) => _menuApi = menuApi;
    #endregion

    #region Ações
    // GET: /Menu/Index
    public async Task<IActionResult> Index()
    {
        var all = await _menuApi.GetAllAsync();

        var today = DateTime.Today;
        var diff = (int)today.DayOfWeek;
        var monday = diff == 0 ? today.AddDays(-6) : today.AddDays(1 - diff);

        var vm = new WeeklyMenuViewModel { WeekStart = monday.Date };

        for (int i = 0; i < 5; i++)
        {
            var day = monday.AddDays(i).Date;
            var menusForDay = all.Where(m => m.Date?.Date == day).ToList();

            var normal = menusForDay.FirstOrDefault(m => m.Type == false || m.Type == null);
            var veg = menusForDay.FirstOrDefault(m => m.Type == true);

            vm.Days.Add(new DayMenu
            {
                Date = day,
                Normal = normal,
                Vegetarian = veg
            });
        }

        return View(vm);
    }
    #endregion

    #region Helpers
    // helpers se necessário (formatos, permissões)
    #endregion
}