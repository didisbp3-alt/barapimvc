using DTO_MVCAPIContracts.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WEBAPITest.Data;
using WEBAPITest.Models;

namespace YourApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly diogoportela_SchoolBarContext _db;
        public ProductsController(diogoportela_SchoolBarContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductsDto>>> GetAll()
        {
            var items = await _db.Products.AsNoTracking()
                .Select(e => ToDto(e))
                .ToListAsync();
            return Ok(items);
        }

        // GET api/Products/search?categoryId=1&maxPrice=5.0&allergens=gluten&q=sandes&onlyActive=true
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProductsDto>>> Search([FromQuery] int? categoryId, [FromQuery] decimal? maxPrice,
            [FromQuery] string? allergens, [FromQuery] string? q, [FromQuery] bool onlyActive = false)
        {
            var query = _db.Products.AsNoTracking().AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CatId == categoryId.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => (p.Price ?? 0m) <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(allergens))
            {
                var terms = allergens.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var term in terms)
                {
                    query = query.Where(p => p.Allergens != null && EF.Functions.Like(p.Allergens, $"%{term}%"));
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(p => (p.Name != null && EF.Functions.Like(p.Name, $"%{term}%"))
                                      || (p.Description != null && EF.Functions.Like(p.Description, $"%{term}%")));
            }

            if (onlyActive)
                query = query.Where(p => p.IsActive);

            var items = await query.Select(p => ToDto(p)).ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductsDto>> GetById(int id)
        {
            var e = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProdId == id);
            if (e == null) return NotFound();
            return Ok(ToDto(e));
        }

        [HttpPost]
        [Authorize(Policy = "BarOrAdmin")]
        public async Task<ActionResult<ProductsDto>> Create(ProductsDto dto)
        {
            var entity = FromDto(dto);
            _db.Products.Add(entity);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.ProdId }, ToDto(entity));
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "BarOrAdmin")]
        public async Task<IActionResult> Update(int id, ProductsDto dto)
        {
            if (id != dto.ProdId) return BadRequest("ID mismatch.");
            var exists = await _db.Products.AnyAsync(p => p.ProdId == id);
            if (!exists) return NotFound();

            var entity = FromDto(dto);
            _db.Products.Update(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "BarOrAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Products.FindAsync(id);
            if (entity == null) return NotFound();
            _db.Products.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static ProductsDto ToDto(Products e) => new()
        {
            ProdId = e.ProdId,
            CatId = e.CatId,
            Name = e.Name ?? string.Empty,
            Description = e.Description,
            Price = e.Price,
            DiscountPercent = e.DiscountPercent ?? 0,
            MaxStock = e.MaxStock ?? 0,
            CurStock = e.CurStock ?? 0,
            Kcal = e.Kcal ?? 0,
            Protein = e.Protein ?? 0m,
            Fat = e.Fat ?? 0m,
            Carbs = e.Carbs ?? 0m,
            Salt = e.Salt ?? 0m,
            Allergens = e.Allergens,
            ImgPath = e.ImgPath,
            IsActive = e.IsActive
        };

        private static Products FromDto(ProductsDto d) => new()
        {
            ProdId = d.ProdId,
            CatId = d.CatId,
            Name = d.Name,
            Description = d.Description,
            Price = d.Price,
            DiscountPercent = d.DiscountPercent,
            MaxStock = d.MaxStock,
            CurStock = d.CurStock,
            Kcal = d.Kcal,
            Protein = d.Protein,
            Fat = d.Fat,
            Carbs = d.Carbs,
            Salt = d.Salt,
            Allergens = d.Allergens,
            ImgPath = d.ImgPath,
            IsActive = d.IsActive ?? false
        };
    }
}


