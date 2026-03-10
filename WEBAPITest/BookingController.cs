using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Services;
using DTO_MVCAPIContracts.Contracts;

namespace MVCAPIFriedBananas.Controllers;

public class BookingController : Controller
{
    #region Dependências
    private readonly BookingApiClient _bookingApi;

    public BookingController(BookingApiClient bookingApi) => _bookingApi = bookingApi;
    #endregion

    #region Ações Públicas
    // GET: /Booking
    public async Task<IActionResult> Index()
    {
        var list = await _bookingApi.GetMyBookingsAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(int menuId)
    {
        var (success, message) = await _bookingApi.BookAsync(menuId);
        TempData["BookingMessage"] = message;
        TempData["BookingSuccess"] = success;
        return RedirectToAction("Index", "Menu");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int bookingId)
    {
        var (success, message) = await _bookingApi.CancelAsync(bookingId);
        TempData["BookingMessage"] = message;
        TempData["BookingSuccess"] = success;
        return RedirectToAction("Index");
    }
    #endregion

    #region Helpers
    // validações locais/format helpers
    #endregion
}