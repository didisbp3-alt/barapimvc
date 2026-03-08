using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WEBAPITest.Data;
using System.Security.Claims;

namespace WEBAPITest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly diogoportela_SchoolBarContext _context;

        public UsersController(diogoportela_SchoolBarContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Balance = user.Balance
            });
        }
    }
}