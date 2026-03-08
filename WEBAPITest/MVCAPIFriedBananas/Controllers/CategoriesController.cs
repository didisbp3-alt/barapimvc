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
        } //

        // GET: /Categories
        public async Task<IActionResult> Index()
        {
            Console.WriteLine("[CATEGORIES] Iniciando Index - verificando token na sessão...");
            var savedToken = HttpContext.Session.GetString("JwtToken");
            Console.WriteLine($"[CATEGORIES] Token na sessão ao entrar no Index: {(savedToken != null ? savedToken.Substring(0, Math.Min(30, savedToken.Length)) + "..." : "NENHUM TOKEN!")}");

            var categories = await _apiClient.GetCategoriesAsync();

            Console.WriteLine($"[CATEGORIES] Categorias carregadas: {categories.Count} itens");

            return View(categories);
        }
        // GET: /Categories/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }
        // POST: /Categories/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var success = await _apiClient.CreateCategoryAsync(model);//erro 
            if (!success)
            {
                ModelState.AddModelError("", "Erro ao criar categoria na API.");
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }
        // GET: /Categories/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _apiClient.GetCategoryAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
        }
        // POST: /Categories/Delete/5
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
        // GET: /Categories/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _apiClient.GetCategoryAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
        }
        // POST: /Categories/Edit/5
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















































































































































































/* 
 
Kyouka Suigetsu... Yokuzo watashi wa souro sossaiati

⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣴⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣴⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠋⢻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⣺⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⣯⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣦⣿⣽⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡼⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠿⠿⠿⠿⠿⠿⠻⠿⢿⣿⡿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀⢹⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠸⣿⠻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣰⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢿⣦⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠐⠛⣩⡿⠋⣿⡿⣿⣿⣿⣿⣿⣿⣿⣿⣀⣀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣿⣻⣿⣿⣿⠙⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⡟⠁⠀⣿⣷⣿⣿⣿⣿⣿⣿⣿⣷⡶⣯⣽⣿⣷⣶⣦⣤⣤⣄⣀⠀⠘⣿⣿⣿⣿⣿⣧⣤⣽⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣼⠁⠀⠀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣽⠿⣶⣾⣭⣭⣭⣙⣻⣟⡻⣿⣆⣿⣿⠈⢻⣿⣿⡛⢟⣛⣋⣙⣿⣛⣿⣿⣛⣿⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣯⠉⠛⠃⠙⠛⠛⠓⠛⠛⠻⠓⠈⣿⣿⣿⣿⣾⡿⣿⣿⡏⠙⠿⠿⠭⠚⠛⢻⡋⠁⢹⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣾⣿⢿⣿⣿⣿⣿⣿⣿⣿⣛⡷⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⢩⠏⠈⢹⣿⠈⠻⣟⡂⠀⠀⠀⠀⠀⢸⠇⠀⣾⢿⣿⢿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠴⠟⠋⠀⣾⣿⢻⣿⣾⣿⣿⡿⢷⣤⣄⣀⣀⣀⣀⣀⣀⣀⣀⣰⣿⠧⠎⠀⠀⠀⣿⣷⣄⡈⠙⠦⠀⠀⠀⠀⣀⣀⣼⢏⣿⢟⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢯⡿⠈⣿⣷⣹⡿⠁⠀⠈⠉⠉⠙⠛⠛⠛⠛⠛⠛⠛⠁⠀⠀⠀⠀⠀⠈⠙⠛⠛⠛⠛⠚⠛⠛⠛⠛⠉⠁⣸⢯⣾⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠿⠀⣿⣿⣿⣧⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣤⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣷⣿⣿⡟
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⣿⢻⣿⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢹⠂⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⣿⣿⡟⣇
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⣿⠈⢿⣿⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡞⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⣿⡟⠀⠙
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠘⠃⠀⣿⣷⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠓⠦⣰⣷⠞⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣾⣿⣿⡇⠀⠀
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⣿⢳⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣿⢻⣿⣿⡇⠀⠀
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢰⣿⣿⡇⢻⡄⠀⠀⠰⢤⣀⡀⣀⣀⣀⣴⡦⠤⠤⠤⠀⠀⠀⠀⢀⣠⡤⠀⣰⣿⡇⢸⣿⣿⣷⠀⠀
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠞⣿⣿⣷⠀⢿⣇⠀⠀⠀⠀⢨⡙⠓⠲⠤⠭⠭⠭⠭⠭⠗⠛⣋⡁⠀⠀⣼⣿⣿⠀⣿⣿⣿⠈⠧⠀
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡴⠋⠀⢿⣿⡿⡄⠘⢿⣷⣄⠀⠀⠀⠙⠓⠦⣤⣤⣤⣤⡤⠤⠔⠚⠉⠀⢠⣾⣿⣿⠁⢰⡟⣿⣇⠀⠀⠀
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣼⣿⡇⣧⠀⠘⢿⣿⢦⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣴⣿⣿⣿⠃⠀⢸⡇⣿⣿⣇⠀⠀
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⣿⣿⢠⢿⠀⠀⠈⣿⣾⣳⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣾⡿⢿⣿⠇⠀⠀⢸⡇⣿⣿⣿⡄⠀
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣼⣿⣿⣿⢨⢸⡆⠀⠀⠘⢿⣿⣺⣧⣄⣀⠀⠀⠀⠀⠀⣠⣴⣿⣿⣷⡿⠁⠀⠀⠀⠀⡇⢹⣿⣿⣿⣶
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⣴⣾⣿⣿⣿⣿⣿⡇⢸⠸⠇⠀⠀⠀⠈⠻⣿⣇⣷⠞⢿⡿⣽⣿⣿⣿⣿⣿⡿⠛⠀⠀⠀⠀⠀⢀⡇⢸⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⣤⣶⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⢸⠀⠀⠀⠀⠀⠀⠀⠈⠻⠿⢿⣼⣧⡿⣀⣿⣿⣿⡟⠀⠀⠀⠀⠀⠀⠀⢸⠇⢰⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣤⣶⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⢴⠀⠈⠀⠀⣩⣿⣿⡟⠀⠀⠀⠀⠀⢰⡇⠀⡸⠀⣾⣿⣿⣿⣿
⠀⠀⠀⠀⠀⠀⠀⠀⢀⣀⣤⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⠘⡇⠀⠀⠀⠀⠀⠀⠀⠀⠈⢳⣴⣤⣴⣿⡿⠉⠀⠀⠀⠀⠀⠀⣾⠁⠀⡇⠀⣿⣿⣿⣿⣿
⠀⠀⠀⠀⢀⣠⣴⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⠀⣧⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠹⣿⣿⠟⠁⠀⠀⠀⠀⠀⠀⣰⡏⠀⣸⠁⢠⣿⣿⣿⣿⣿
⢀⣠⣴⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⠀⢻⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⠇⢀⠏⠀⣼⣿⣿⣿⣿⣿
⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣇⠀⢸⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣾⣋⠀⡼⠀⢠⣿⣿⣿⣿⣿⣿


 
ps: o professor nunca vai saber.. 😊
 
 */