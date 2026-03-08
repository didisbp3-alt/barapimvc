using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Services;

namespace MVCAPIFriedBananas.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly BookingApiClient _bookings;

        public BookingController(BookingApiClient bookings)
        {
            _bookings = bookings;
        }

        // GET: /Booking  — user's booking list
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var list = await _bookings.GetMyBookingsAsync();
                return View(list);
            }
            catch
            {
                TempData["BookingMessage"] = "Não foi possível carregar as marcações.";
                TempData["BookingSuccess"] = false;
                return View(new List<DTO_MVCAPIContracts.Contracts.LunchBookingDto>());
            }
        }

        // POST: /Booking/Book — create a booking for a menu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int menuId, DateOnly? weekStart)
        {
            var (success, message) = await _bookings.BookAsync(menuId);

            TempData["BookingSuccess"] = success;
            TempData["BookingMessage"] = ParseMessage(message, success
                ? "Reserva efetuada com sucesso! ✅"
                : "Não foi possível fazer a reserva.");

            return RedirectToAction("Index", "Menu", new { weekStart });
        }

        // POST: /Booking/Cancel — cancel a booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int bookingId, string? returnUrl)
        {
            var (success, message) = await _bookings.CancelAsync(bookingId);

            TempData["BookingSuccess"] = success;
            TempData["BookingMessage"] = ParseMessage(message, success
                ? "Marcação cancelada com sucesso."
                : "Não foi possível cancelar a marcação.");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // Extract a clean message from the API response (strips JSON wrapping if present)
        private static string ParseMessage(string raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            // API may return {"message":"..."} — try to extract the inner text
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("message", out var prop))
                    return prop.GetString() ?? fallback;
            }
            catch (System.Text.Json.JsonException)
            {
                /* not JSON — return raw string as-is */
            }
            // Strip outer quotes if the whole thing is a JSON string
            return raw.Trim('"', ' ');
        }
    }
}
