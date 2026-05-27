using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace PainelControlador.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        return Ok(new
        {
            status = "ok",
            version,
            timestamp = DateTime.UtcNow
        });
    }
}
