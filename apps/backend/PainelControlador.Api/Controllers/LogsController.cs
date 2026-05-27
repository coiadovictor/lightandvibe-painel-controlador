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
    private readonly IN8nDbService _n8n;

    public LogsController(ILogService service, IN8nDbService n8n)
    {
        _service = service;
        _n8n = n8n;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var stats = await _n8n.GetLogStatsAsync(ct);
        return Ok(stats);
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] int limit = 100, CancellationToken ct = default)
    {
        if (limit > 500) limit = 500;
        var messages = await _n8n.GetRecentMessagesAsync(limit, ct);
        return Ok(messages);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var logs = await _service.ListAsync(ct);
        return Ok(logs);
    }
}
