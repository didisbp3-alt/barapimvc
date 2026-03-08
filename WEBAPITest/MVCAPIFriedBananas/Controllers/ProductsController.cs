using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Models;
using MVCAPIFriedBananas.Services;
using MVCAPIFriedBananas.ViewModels;
using MVCAPIFriedBananas.Views.ViewModels;
using System.Linq;

namespace MVCAPIFriedBananas.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductsApiClient _products;
        private readonly OrderApiClient _orders;
        private readonly CategoriesApiClient _categories;

        public ProductsController(ProductsApiClient products, OrderApiClient orders, CategoriesApiClient categoriesApiClient)
        {
            _products = products;
            _orders = orders;
            _categories = categoriesApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ProductsPageViewModel query)
        {
            var products = (await _products.GetProductsAsync())
                .Where(p => p.ProductType == 0) // Only FOOD items
                .ToList();

            var order = await _orders.GetCartAsync();
            var cartCount = order?.Items?.Sum(i => i.Quantity) ?? 0;

            var categoriesList = await _categories.GetCategoriesAsync();
            var categories = categoriesList
                .Select(c => c.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            var allergens = products
                .SelectMany(p => SplitAllergens(p.Allergens))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a)
                .ToList();

            var filtered = ApplyFilters(products, query);

            var vm = new ProductsPageViewModel
            {
                Categories = categories,
                Allergens = allergens,
                SelectedCategory = query.SelectedCategory,
                SelectedAllergen = query.SelectedAllergen,
                PriceRange = query.PriceRange,
                Search = query.Search,
                Items = filtered.Select(p => ToCard(p, categoriesList)).ToList(),
                CartCount = cartCount
            };

            return View(vm);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AdminIndex([FromQuery] ProductsPageViewModel query)
        {
            // Get all products (not just FOOD)
            var products = (await _products.GetProductsAsync()).ToList();

            var order = await _orders.GetCartAsync();
            var cartCount = order?.Items?.Sum(i => i.Quantity) ?? 0;

            var categoriesList = await _categories.GetCategoriesAsync();
            var categories = categoriesList
                .Select(c => c.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            var allergens = products
                .SelectMany(p => SplitAllergens(p.Allergens))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a)
                .ToList();

            var filtered = ApplyFilters(products, query);

            var vm = new ProductsPageViewModel
            {
                Categories = categories,
                CategoryList = categoriesList,
                Allergens = allergens,
                SelectedCategory = query.SelectedCategory,
                SelectedAllergen = query.SelectedAllergen,
                PriceRange = query.PriceRange,
                Search = query.Search,
                Items = filtered.Select(p => ToCard(p, categoriesList)).ToList(),
                CartCount = cartCount
            };

            return View("AdminIndex", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Filter([FromQuery] ProductsPageViewModel query)
        {
            var products = (await _products.GetProductsAsync())
                .Where(p => p.ProductType == 0) // Only FOOD items
                .ToList();

            var categoriesList = await _categories.GetCategoriesAsync();

            var filtered = ApplyFilters(products, query)
                .Select(p => ToCard(p, categoriesList))
                .ToList();

            return PartialView("_ProductCard", filtered);
        }

        public async Task<IActionResult> Details(int id)
        {
            var p = await _products.GetProductAsync(id);
            if (p == null) return NotFound();

            var categoriesList = await _categories.GetCategoriesAsync();
            var categoryName = categoriesList.FirstOrDefault(c => c.CatId == p.CatId)?.Name ?? $"Categoria {p.CatId}";

            var vm = new ProductsViewModel
            {
                ProdId = p.ProdId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price ?? 0m,
                StockCurrent = p.CurStock,
                StockMax = p.MaxStock,
                ImageUrl = p.ImgPath,
                DiscountPercent = p.DiscountPercent,
                ProductType = p.ProductType,
                CategoryName = categoryName
            };
            return View(vm);
        }

        private static IEnumerable<Product> ApplyFilters(IEnumerable<Product> products, ProductsPageViewModel q)
        {
            var list = products;

            if (!string.IsNullOrWhiteSpace(q.SelectedCategory))
                list = list.Where(p =>
                    string.Equals(p.CategoryName, q.SelectedCategory, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals($"Categoria {p.CatId}", q.SelectedCategory, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (!string.IsNullOrWhiteSpace(q.SelectedAllergen))
                list = list.Where(p => SplitAllergens(p.Allergens)
                    .Contains(q.SelectedAllergen!, StringComparer.OrdinalIgnoreCase))
                    .ToList();

            if (!string.IsNullOrWhiteSpace(q.Search))
                list = list.Where(p =>
                    (!string.IsNullOrWhiteSpace(p.Name) && p.Name.Contains(q.Search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(p.Description) && p.Description.Contains(q.Search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            if (!string.IsNullOrWhiteSpace(q.PriceRange))
            {
                list = q.PriceRange switch
                {
                    "lt5" => list.Where(p => (p.Price ?? 0m) < 5m),
                    "5to10" => list.Where(p => (p.Price ?? 0m) >= 5m && (p.Price ?? 0m) <= 10m),
                    "gt10" => list.Where(p => (p.Price ?? 0m) > 10m),
                    _ => list
                };
            }

            return list;
        }

        private static ProductsViewModel ToCard(Product p, List<Category> categories)
        {
            var categoryName = categories.FirstOrDefault(c => c.CatId == p.CatId)?.Name ?? $"Categoria {p.CatId}";
            return new ProductsViewModel
            {
                ProdId = p.ProdId,
                Name = p.Name,
                Description = p.Description ?? string.Empty,
                Price = p.Price ?? 0m,
                OriginalPrice = null,
                DiscountPercent = p.DiscountPercent,
                StockCurrent = p.CurStock,
                StockMax = p.MaxStock,
                ImageUrl = p.ImgPath,
                IsFavorite = p.IsFavorite,
                Cat_Id = p.CatId,
                ProductType = p.ProductType,
                CategoryName = categoryName
            };
        }

        private static IEnumerable<string> SplitAllergens(string allergens)
        {
            if (string.IsNullOrWhiteSpace(allergens))
                return Enumerable.Empty<string>();

            return allergens
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a));
        }

        // GET: Products/Create
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            // Optionally, pass categories for dropdown
            var categories = await _categories.GetCategoriesAsync();
            ViewBag.Categories = categories;
            return View(new ProductsViewModel());
        }

        // POST: Products/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _categories.GetCategoriesAsync();
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                CatId = model.Cat_Id ?? 0,
                MaxStock = model.StockMax ?? 0,
                CurStock = model.StockCurrent ?? 0,
                DiscountPercent = model.DiscountPercent ?? 0,
                ImgPath = model.ImageUrl,
                ProductType = model.ProductType,
                IsFavorite = model.IsFavorite,
                IsActive = true
            };

            var created = await _products.CreateProductAsync(product);
            if (created == null)
            {
                ModelState.AddModelError("", "Erro ao criar produto.");
                ViewBag.Categories = await _categories.GetCategoriesAsync();
                return View(model);
            }

            return RedirectToAction(nameof(AdminIndex));
        }

        // GET: Products/Edit/{id}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _products.GetProductAsync(id);
            if (product == null) return NotFound();

            var categories = await _categories.GetCategoriesAsync();
            ViewBag.Categories = categories;

            var vm = ToCard(product, categories);
            return View(vm);
        }

        // POST: Products/Edit/{id}
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductsViewModel model)
        {
            if (id != model.ProdId) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _categories.GetCategoriesAsync();
                return View(model);
            }

            var product = new Product
            {
                ProdId = model.ProdId,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                CatId = model.Cat_Id ?? 0,
                MaxStock = model.StockMax ?? 0,
                CurStock = model.StockCurrent ?? 0,
                DiscountPercent = model.DiscountPercent ?? 0,
                ImgPath = model.ImageUrl,
                ProductType = model.ProductType,
                IsFavorite = model.IsFavorite,
                IsActive = true
            };

            var success = await _products.UpdateProductAsync(product);
            if (!success)
            {
                ModelState.AddModelError("", "Erro ao atualizar produto.");
                ViewBag.Categories = await _categories.GetCategoriesAsync();
                return View(model);
            }

            return RedirectToAction(nameof(AdminIndex));
        }

        // GET: Products/Delete/{id}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _products.GetProductAsync(id);
            if (product == null) return NotFound();

            var categories = await _categories.GetCategoriesAsync();
            var vm = ToCard(product, categories);
            return View(vm);
        }

        // POST: Products/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _products.DeleteProductAsync(id);
            if (!success)
            {
                ModelState.AddModelError("", "Erro ao eliminar produto.");
                return RedirectToAction(nameof(AdminIndex));
            }
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}