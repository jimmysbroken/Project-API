using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("pong");
    }

    [HttpGet("environment")]
    public IActionResult GetEnvironment([FromServices] IHostEnvironment env)
    {
        return Ok(new
        {
            Environment = env.EnvironmentName,
            ApplicationName = env.ApplicationName,
            MachineName = Environment.MachineName
        });
    }
}
