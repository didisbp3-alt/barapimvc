using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    UId = u.UId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    Balance = u.Balance
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                UId = user.UId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Balance = user.Balance
            });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Delete related LunchBookings first (FK is non-nullable, cannot be nulled out)
                var lunchBookings = await _context.LunchBookings
                    .Where(lb => lb.UId == id)
                    .ToListAsync();
                _context.LunchBookings.RemoveRange(lunchBookings);

                // Null out related Orders (UserId is nullable, preserve order history)
                var orders = await _context.Orders
                    .Where(o => o.UserId == id)
                    .ToListAsync();
                foreach (var order in orders)
                    order.UserId = null;

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return NoContent();
        }
    }
}