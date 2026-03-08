using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using MVCAPIFriedBananas.Models;
using MVCAPIFriedBananas.Services; // Se precisar do ApiClient

namespace MVCAPIFriedBananas.Controllers
{
    public class BookingController : Controller
    {
        private readonly MenuApiClient _menus;
        public BookingController(MenuApiClient menus)
        {
            _menus = menus;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int menuId, DateOnly? weekStart)
        {
            var booking = await _menus.BookAsync(menuId);
            var ok = booking is not null; // or booking?.Success == true if you add Success
            TempData["BookingSuccess"] = ok;
            TempData["BookingMessage"] = ok ? "Reserva efetuada com sucesso." : "Falha ao reservar.";
            return RedirectToAction("Index", "Menu", new { weekStart });
        }
    }
}