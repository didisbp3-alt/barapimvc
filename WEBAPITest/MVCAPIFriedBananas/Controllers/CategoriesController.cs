using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Services;
using MVCAPIFriedBananas.Models;

namespace MVCAPIFriedBananas.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly CategoriesApiClient _apiClient;

        public CategoriesController(CategoriesApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _apiClient.GetCategoriesAsync();
            return View(categories);
        }

        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var success = await _apiClient.CreateCategoryAsync(model);
            if (!success)
            {
                ModelState.AddModelError("", "Erro ao criar categoria na API.");
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _apiClient.GetCategoryAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _apiClient.DeleteCategoryAsync(id);
            if (!success)
            {
                ModelState.AddModelError("", "Erro ao eliminar categoria na API.");
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _apiClient.GetCategoryAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.CatId)
                return BadRequest();
            if (!ModelState.IsValid)
                return View(model);

            var success = await _apiClient.UpdateCategoryAsync(model);
            if (!success)
            {
                ModelState.AddModelError("", "Erro ao atualizar categoria na API.");
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
