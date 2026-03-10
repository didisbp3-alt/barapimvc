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
    private readonly IWebHostEnvironment _env;

    public ProductsAdminController(diogoportela_SchoolBarContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

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

    // POST api/ProductsAdmin/{id}/upload-image
    [HttpPost("{id:int}/upload-image")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "Ficheiro inválido." });

        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProdId == id);
        if (product == null) return NotFound(new { message = "Produto não encontrado." });

        var uploads = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "products");
        Directory.CreateDirectory(uploads);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploads, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        // guarda caminho relativo para servir static files (ex: /uploads/products/xyz.jpg)
        product.ImgPath = $"/uploads/products/{fileName}";
        await _db.SaveChangesAsync();

        return Ok(new { prodId = product.ProdId, imgPath = product.ImgPath });
    }
}