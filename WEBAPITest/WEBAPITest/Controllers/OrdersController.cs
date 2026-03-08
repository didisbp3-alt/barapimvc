using System.Security.Claims;
using DTO_MVCAPIContracts.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WEBAPITest.Services;

namespace WEBAPITest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _orders;

    public OrdersController(IOrdersService orders) => _orders = orders;

    // ---------- Cart (current open order) ----------

    // GET api/Orders/cart
    [HttpGet("cart")]
    [Authorize]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var res = await _orders.GetOrCreateCartAsync(userId.Value); // add to service
        if (!res.Success) return BadRequest(new { message = res.Message });
        return Ok(res.Data);
    }

    // POST api/Orders/add
    [HttpPost("add")]
    [Authorize]
    public async Task<IActionResult> Add([FromBody] AddOrderItemRequest req)
    {
        if (req is null || req.ProductId <= 0 || req.Qty <= 0)
            return BadRequest(new { message = "Invalid product or quantity." });

        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var res = await _orders.AddItemAsync(userId.Value, req.ProductId, req.Qty); // add to service
        if (!res.Success) return BadRequest(new { message = res.Message });
        return Ok(res.Data); // return updated OrderDto (cart)
    }

    // POST api/Orders/update
    [HttpPost("update")]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateOrderItemRequest req)
    {
        if (req is null || req.ProductId <= 0 || req.Qty < 0)
            return BadRequest(new { message = "Invalid product or quantity." });

        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var res = await _orders.UpdateItemAsync(userId.Value, req.ProductId, req.Qty); // add to service
        if (!res.Success) return BadRequest(new { message = res.Message });
        return Ok(res.Data); // updated cart
    }

    // POST api/Orders/remove
    [HttpPost("remove")]
    [Authorize]
    public async Task<IActionResult> Remove([FromBody] RemoveOrderItemRequest req)
    {
        if (req is null || req.ProductId <= 0)
            return BadRequest(new { message = "Invalid product." });

        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var res = await _orders.RemoveItemAsync(userId.Value, req.ProductId); // add to service
        if (!res.Success) return BadRequest(new { message = res.Message });
        return Ok(res.Data); // updated cart
    }

    // ---------- Checkout & history ----------

    // POST api/Orders/checkout
    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> Checkout([FromBody] OrderDto dto)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var res = await _orders.CreateOrderAsync(userId.Value, dto);
        if (!res.Success) return BadRequest(new { message = res.Message });
        return Ok(new { orderId = res.Data, message = res.Message });
    }

    // GET api/Orders/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var res = await _orders.GetUserOrdersAsync(userId.Value);
        if (!res.Success) return BadRequest(new { message = res.Message });
        return Ok(res.Data);
    }

    // GET api/Orders/{id}
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var res = await _orders.GetOrderByIdAsync(userId.Value, id);
        if (!res.Success) return NotFound(new { message = res.Message });
        return Ok(res.Data);
    }

    private int? GetUserIdFromClaims()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out var id) ? id : null;
    }
}

// Request DTOs for cart operations
public sealed record AddOrderItemRequest(int ProductId, int Qty = 1);
public sealed record UpdateOrderItemRequest(int ProductId, int Qty);
public sealed record RemoveOrderItemRequest(int ProductId);