using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Services;

namespace WebAPI.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly DatabaseUserService _userService;
    private readonly IConfiguration _configuration;

    public AuthController(DatabaseUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var usuario = await _userService.FindByUsernameAsync(request.Username);

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (usuario.Passwd != request.Password)
        {
            return Unauthorized();
        }

        var user = new User(usuario.Id, usuario.Username, usuario.Passwd, new[] { usuario.Rol });
        var token = JwtTokenService.GenerateJwtToken(user, _configuration);

        return Ok(new LoginResponse(token));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetMe()
    {
        var username = User.Identity?.Name;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var userId = User.FindAll(ClaimTypes.NameIdentifier)
            .Select(c => c.Value)
            .FirstOrDefault(v => int.TryParse(v, out _));
        var allClaims = User.Claims.Select(c => new { c.Type, c.Value });

        return Ok(new
        {
            username,
            userId,
            roles,
            allClaims
        });
    }

    [HttpGet("secret")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult GetAdminSecret()
    {
        return Ok("Sólo los admins pueden ver esto.");
    }
}
