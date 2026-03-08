using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Models;
using MVCAPIFriedBananas.Services;

namespace MVCAPIFriedBananas.Controllers
{
    public class UsersController : Controller
    {
        private readonly UsersApiClient _usersApiClient;

        public UsersController(UsersApiClient usersApiClient)
        {
            _usersApiClient = usersApiClient;
        }

        public async Task<IActionResult> Details()
        {
            var currentUser = await _usersApiClient.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var viewModel = new UserViewModel
            {
                FullName = currentUser.FullName ?? "Nome não definido",
                Email = currentUser.Email ?? "Email não definido",
                Role = currentUser.Role switch
                {
                    0 => "Administrador",
                    1 => "Funcionário / Bar",
                    2 => "Aluno",
                    _ => "Utilizador"
                },
                Balance = currentUser.Balance ?? 0m
            };

            return View(viewModel);
        }
    }
}