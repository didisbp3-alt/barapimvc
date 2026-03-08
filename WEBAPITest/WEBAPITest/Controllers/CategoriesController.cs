using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using WEBAPITest.Data;
using WEBAPITest.Models;
namespace APIBarEscola.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly diogoportela_SchoolBarContext _context;
        public CategoriesController(diogoportela_SchoolBarContext context)
        {
            _context = context;
        }
        // GET: api/CATEGORIES
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Category>>> GetCATEGORIES()
        {
            return await _context.Categories
            .AsNoTracking()
            .ToListAsync();
        }
        // GET: api/CATEGORIES/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Category>> GetCATEGORY(int id)
        {
            var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CatId == id);
            if (category == null)
                return NotFound();
            return category;
        }
        // POST: api/CATEGORIES
        [HttpPost]
        [Authorize(Policy = "BarOrAdmin")]
        public async Task<ActionResult<Category>> PostCATEGORY(Category category)
        {
            category.CatId = 0;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCATEGORY), new { id = category.CatId }, category);
        }
        // PUT: api/CATEGORIES/5
        [HttpPut("{id}")]
        [Authorize(Policy = "BarOrAdmin")]
        public async Task<IActionResult> PutCATEGORY(int id, Category category)
        {
            if (id != category.CatId)
            {
                return BadRequest("O id da URL é diferente do id do objeto.");
            }
            _context.Entry(category).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                    return NotFound();
                else
                    throw;
            }
            return NoContent();
        }
        // DELETE: api/CATEGORIES/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "BarOrAdmin")]
        public async Task<IActionResult> DeleteCATEGORY(int id)
        {
            var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CatId == id);
            if (category == null)
            {
                return NotFound();
            }
            if (category.Products != null && category.Products.Any())
            {
                return BadRequest("Não é possível eliminar a categoria: existem produtos associados.");
            }
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CatId == id);
        }
    }
}
