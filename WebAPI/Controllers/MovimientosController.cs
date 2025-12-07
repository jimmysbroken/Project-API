using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minimalapi.JWT.Models;
using Minimalapi.JWT.Services;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/movimientos")]
[Authorize]
public class MovimientosController : ControllerBase
{
    private readonly MovimientoService _movimientoService;

    public MovimientosController(MovimientoService movimientoService)
    {
        _movimientoService = movimientoService;
    }

    [HttpPost]
    public async Task<IActionResult> RegistrarMovimiento([FromBody] RegistrarMovimientoDTO dto)
    {
        var userIdClaim = User.FindAll(ClaimTypes.NameIdentifier)
            .Select(c => c.Value)
            .FirstOrDefault(v => int.TryParse(v, out _));

        if (userIdClaim == null || !int.TryParse(userIdClaim, out var usuarioId))
        {
            return BadRequest(new { error = "Invalid user ID in token" });
        }

        var movimientoId = await _movimientoService.RegistrarMovimientoAsync(dto, usuarioId);
        return CreatedAtAction(nameof(GetById), new { id = movimientoId }, new { id = movimientoId });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var movimientos = await _movimientoService.GetAllAsync();
        return Ok(movimientos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var movimiento = await _movimientoService.GetByIdAsync(id);
        return movimiento is not null ? Ok(movimiento) : NotFound();
    }

    [HttpGet("{id}/detalles")]
    public async Task<IActionResult> GetDetalles(int id)
    {
        var detalles = await _movimientoService.GetDetallesAsync(id);
        return Ok(detalles);
    }
}
