using System.Security.Claims;
using DTO_MVCAPIContracts.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WEBAPITest.Services;

namespace WEBAPITest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LunchBookingsController : ControllerBase
{
    private readonly ILunchBookingService _service;

    public LunchBookingsController(ILunchBookingService service)
    {
        _service = service;
    }

    // POST api/LunchBookings/book/{menuId}
    [HttpPost("book/{menuId}")]
    [Authorize]
    public async Task<IActionResult> Book(int menuId)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var result = await _service.BookLunchAsync(userId.Value, menuId);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    // POST api/LunchBookings/cancel/{bookingId}
    [HttpPost("cancel/{bookingId}")]
    [Authorize]
    public async Task<IActionResult> Cancel(int bookingId)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var result = await _service.CancelBookingAsync(userId.Value, bookingId);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    // GET api/LunchBookings/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> MyBookings()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var list = await _service.GetUserBookingsAsync(userId.Value);
        return Ok(list);
    }

    private int? GetUserIdFromClaims()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out var id)) return id;
        return null;
    }
}