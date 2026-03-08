using DTO_MVCAPIContracts.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Services;
using MVCAPIFriedBananas.ViewModels;
using MVCAPIFriedBananas.Views.ViewModels;

namespace MVCAPIFriedBananas.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly OrderApiClient _orders;

        public CartController(OrderApiClient orders)
        {
            _orders = orders;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var order = await _orders.GetCartAsync();
            return View(ToVm(order));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, int qty = 1)
        {
            if (qty < 1) qty = 1;
            await _orders.AddItemAsync(id, qty);

            if (IsAjax())
            {
                var order = await _orders.GetCartAsync();
                return Json(new
                {
                    success = true,
                    cartCount = order.Items?.Sum(i => i.Quantity) ?? 0
                });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int qty)
        {
            if (qty < 0) qty = 0;
            await _orders.UpdateItemAsync(id, qty);

            if (IsAjax())
            {
                var order = await _orders.GetCartAsync();
                return Json(new
                {
                    success = true,
                    cartCount = order.Items?.Sum(i => i.Quantity) ?? 0
                });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            await _orders.RemoveItemAsync(id);

            if (IsAjax())
            {
                var order = await _orders.GetCartAsync();
                return Json(new
                {
                    success = true,
                    cartCount = order.Items?.Sum(i => i.Quantity) ?? 0
                });
            }

            return RedirectToAction(nameof(Index));
        }

        private static OrderViewModel ToVm(OrderDto dto)
        {
            var items = dto.Items?.Select(i => new OrderItemViewModel
            {
                ProductId = i.ProductId,
                Name = i.Name,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList() ?? new List<OrderItemViewModel>();

            return new OrderViewModel
            {
                Items = items,
                Subtotal = dto.Subtotal,
                Total = dto.Total ?? dto.Subtotal
            };
        }

        private bool IsAjax() =>
            HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            HttpContext.Request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}