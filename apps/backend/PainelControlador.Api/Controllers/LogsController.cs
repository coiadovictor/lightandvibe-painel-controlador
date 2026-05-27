using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogService _service;

    public LogsController(ILogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var logs = await _service.ListAsync(ct);
        return Ok(logs);
    }
}
