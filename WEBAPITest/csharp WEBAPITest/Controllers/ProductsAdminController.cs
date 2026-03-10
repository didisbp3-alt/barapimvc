using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WEBAPITest.Data;
using WEBAPITest.Models;

namespace WEBAPITest.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BarOrAdmin")]
public class ProductsAdminController : ControllerBase
{
    private readonly diogoportela_SchoolBarContext _db;

    public ProductsAdminController(diogoportela_SchoolBarContext db) => _db = db;

    public sealed record AdjustStockDto(int Delta);

    // PUT api/ProductsAdmin/{id}/adjust-stock
    [HttpPut("{id:int}/adjust-stock")]
    public async Task<IActionResult> AdjustStock(int id, [FromBody] AdjustStockDto dto)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProdId == id);
        if (product == null) return NotFound(new { message = "Produto não encontrado." });

        var cur = product.CurStock ?? 0;
        var updated = cur + dto.Delta;
        if (updated < 0) return BadRequest(new { message = "Stock resultante não pode ser negativo." });

        product.CurStock = updated;
        await _db.SaveChangesAsync();

        return Ok(new { prodId = product.ProdId, curStock = product.CurStock });
    }
}