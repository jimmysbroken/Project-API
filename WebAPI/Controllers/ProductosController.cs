using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minimalapi.JWT.Models;
using Minimalapi.JWT.Services;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize]
public class ProductosController : ControllerBase
{
    private readonly ProductoService _productoService;

    public ProductosController(ProductoService productoService)
    {
        _productoService = productoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var productos = await _productoService.GetAllAsync();
        return Ok(productos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var producto = await _productoService.GetByIdAsync(id);
        return producto is not null ? Ok(producto) : NotFound();
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] ProductoDTO dto)
    {
        var id = await _productoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductoDTO dto)
    {
        var updated = await _productoService.UpdateAsync(id, dto);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _productoService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
