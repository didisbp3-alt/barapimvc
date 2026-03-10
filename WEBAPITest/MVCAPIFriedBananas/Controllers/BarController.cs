using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Services;

namespace MVCAPIFriedBananas.Controllers
{
    [Authorize]
    public class BarController : Controller
    {
        private readonly ProductsApiClient _products;

        public BarController(ProductsApiClient products)
        {
            _products = products;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _products.GetProductsAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _products.GetProductAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }
    }
}
