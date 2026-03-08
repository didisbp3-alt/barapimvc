using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVCAPIFriedBananas.Models;
using MVCAPIFriedBananas.Services; // your Product service abstraction
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MVCAPIFriedBananas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // remove or adjust if you want anonymous access
    public class BarController : ControllerBase
    {
        private readonly IProductsService _service;

        public BarController(IProductsService service)
        {
            _service = service;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        {
            var products = await _service.GetAllAsync();
            return Ok(products);
        }

        // GET: api/Products/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Product>> GetById(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product == null)
                return NotFound();
            return Ok(product);
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> Create(Product product)
        {
            var created = await _service.CreateAsync(product);
            // Returns 201 with Location header
            return CreatedAtAction(nameof(GetById), new { id = created.ProdId }, created);
        }

        // PUT: api/Products/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Product product)
        {
            if (id != product.ProdId)
                return BadRequest("ID mismatch.");

            var updated = await _service.UpdateAsync(product);
            if (!updated)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}