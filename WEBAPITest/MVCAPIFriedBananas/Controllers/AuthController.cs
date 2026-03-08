using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MVCAPIFriedBananas.Controllers
{
    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("BarEscolaApi");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Auth/login", model);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Erro na API: {response.StatusCode} - {errorContent}");
                    return View(model);
                }

                var content = await response.Content.ReadAsStringAsync();

                string? token = null;
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("token", out var p) || root.TryGetProperty("Token", out p))
                            token = p.GetString();
                    }
                }
                catch (JsonException) { }

                if (string.IsNullOrWhiteSpace(token))
                {
                    ModelState.AddModelError(string.Empty, "A API não devolveu um token válido. Tenta novamente.");
                    return View(model);
                }

                HttpContext.Session.SetString("JwtToken", token);
                TempData["SuccessMessage"] = "Login efetuado com sucesso!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Erro inesperado: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JwtToken");
            return RedirectToAction("Login");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Auth/register", model);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Erro: {error}");
                    return View(model);
                }

                var content = await response.Content.ReadFromJsonAsync<dynamic>();
                var token = (string?)content?.Token?.ToString();
                if (string.IsNullOrEmpty(token))
                {
                    ModelState.AddModelError(string.Empty, "Registro OK, mas sem token. Faça login.");
                    return View(model);
                }

                Microsoft.AspNetCore.Http.SessionExtensions.SetString(HttpContext.Session, "JwtToken", token);
                TempData["SuccessMessage"] = "Registro efetuado com sucesso!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Erro: {ex.Message}");
                return View(model);
            }
        }
    }
}
