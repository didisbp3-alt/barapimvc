using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using WEBAPITest.Data;
using WEBAPITest.Models; // Orders, OrderItem, Products
using DTO_MVCAPIContracts.Contracts;

namespace WEBAPITest.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly diogoportela_SchoolBarContext _db; // <-- replace with your DbContext name

        public CartController(diogoportela_SchoolBarContext db)
        {
            _db = db;
        }

        // GET api/cart
        [HttpGet]
        public async Task<ActionResult<OrderDto>> GetCart()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var cart = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Cart");

            if (cart == null) return new OrderDto();

            RecalcOrderTotals(cart);
            return ToDto(cart);
        }

        // POST api/cart/add
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemRequestDTO request)
        {
            if (request == null || request.ProductId <= 0 || request.Qty <= 0)
                return BadRequest("Invalid product or quantity.");

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var product = await _db.Products.FindAsync(request.ProductId);
            if (product == null) return NotFound("Product not found.");

            var cart = await GetOrCreateCart(userId.Value);

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (existing == null)
            {
                cart.Items.Add(new OrderItem
                {
                    ProductId = request.ProductId,
                    Quantity = request.Qty,
                    UnitPrice = product.Price ?? 0m
                });
            }
            else
            {
                existing.Quantity += request.Qty;
            }

            RecalcOrderTotals(cart);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // POST api/cart/update
        [HttpPost("update")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemRequestDTO request)
        {
            if (request == null || request.ProductId <= 0 || request.Qty < 0)
                return BadRequest("Invalid product or quantity.");

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var cart = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Cart");

            if (cart == null) return NotFound("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (item == null) return NotFound("Item not found in cart.");

            if (request.Qty == 0)
                cart.Items.Remove(item);
            else
                item.Quantity = request.Qty;

            RecalcOrderTotals(cart);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // POST api/cart/remove
        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveCartItemRequestDTO request)
        {
            if (request == null || request.ProductId <= 0)
                return BadRequest("Invalid product.");

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var cart = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Cart");

            if (cart == null) return NotFound("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (item == null) return NotFound("Item not found in cart.");

            cart.Items.Remove(item);
            RecalcOrderTotals(cart);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // Helpers
        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var id)) return id;
            return null;
        }

        private async Task<Orders> GetOrCreateCart(int userId)
        {
            var cart = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Cart");

            if (cart != null) return cart;

            cart = new Orders
            {
                UserId = userId,
                Status = "Cart",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Total = 0m,
                Items = new List<OrderItem>()
            };

            _db.Orders.Add(cart);
            await _db.SaveChangesAsync();
            return cart;
        }

        private static void RecalcOrderTotals(Orders order)
        {
            var subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            order.Total = subtotal;
            order.UpdatedAt = DateTime.UtcNow;
        }

        private static OrderDto ToDto(Orders order)
        {
            var items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId ?? 0,
                Name = i.Product?.Name ?? "",
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList();

            var subtotal = items.Sum(x => x.UnitPrice * x.Quantity);
            return new OrderDto
            {
                Items = items,
                Subtotal = subtotal,
                Total = subtotal
            };
        }
    }

}
