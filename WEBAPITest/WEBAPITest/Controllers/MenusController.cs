using DTO_MVCAPIContracts.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WEBAPITest.Data;          // ApplicationDbContext
using WEBAPITest.Models;

namespace WEBAPITest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenusController : ControllerBase
    {
        private readonly diogoportela_SchoolBarContext _db;
        public MenusController(diogoportela_SchoolBarContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenusDto>>> GetAll()
        {
            var items = await _db.Menus.AsNoTracking()
                .Select(m => ToDto(m))
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MenusDto>> GetById(int id)
        {
            var m = await _db.Menus.AsNoTracking()
                .SingleOrDefaultAsync(x => x.MId == id);
            if (m == null) return NotFound();
            return Ok(ToDto(m));
        }

        [HttpPost]
        public async Task<ActionResult<MenusDto>> Create(MenusDto dto)
        {
            var entity = FromDto(dto);
            // keep AvailableSeats in sync if not provided
            if (entity.AvailableSeats == 0 && entity.MaxSeats.HasValue && entity.UsedSeats.HasValue)
                entity.AvailableSeats = entity.MaxSeats.Value - entity.UsedSeats.Value;

            _db.Menus.Add(entity);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.MId }, ToDto(entity));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, MenusDto dto)
        {
            if (id != dto.MId) return BadRequest("ID mismatch.");

            var exists = await _db.Menus.AnyAsync(x => x.MId == id);
            if (!exists) return NotFound();

            var entity = FromDto(dto);
            if (entity.AvailableSeats == 0 && entity.MaxSeats.HasValue && entity.UsedSeats.HasValue)
                entity.AvailableSeats = entity.MaxSeats.Value - entity.UsedSeats.Value;

            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Menus.FindAsync(id);
            if (m == null) return NotFound();
            _db.Menus.Remove(m);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static MenusDto ToDto(Menu m) => new()
        {
            MId = m.MId,
            Date = m.Date,
            Type = m.Type,
            MainDish = m.MainDish ?? string.Empty,
            Soup = m.Soup ?? string.Empty,
            Dessert = m.Dessert ?? string.Empty,
            Notes = m.Notes ?? string.Empty,
            MaxSeats = m.MaxSeats,
            UsedSeats = m.UsedSeats,
            AvailableSeats = m.AvailableSeats
        };

        private static Menu FromDto(MenusDto d) => new()
        {
            MId = d.MId,
            Date = d.Date,
            Type = d.Type,
            MainDish = string.IsNullOrWhiteSpace(d.MainDish) ? null : d.MainDish,
            Soup = string.IsNullOrWhiteSpace(d.Soup) ? null : d.Soup,
            Dessert = string.IsNullOrWhiteSpace(d.Dessert) ? null : d.Dessert,
            Notes = string.IsNullOrWhiteSpace(d.Notes) ? null : d.Notes,
            MaxSeats = d.MaxSeats,
            UsedSeats = d.UsedSeats,
            AvailableSeats = d.AvailableSeats // required, ensure provided or computed
        };
    }
}
