using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

/// <summary>
/// Logs Internos do Ambiente — saúde e falhas dos containers do stack (read-only).
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AmbienteController : ControllerBase
{
    private readonly IDockerLogsService _docker;

    public AmbienteController(IDockerLogsService docker) => _docker = docker;

    /// <summary>
    /// Saúde dos containers monitorados + linha do tempo de incidentes na janela
    /// (em horas, padrão 48, máx. 48).
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] int hours = 48, CancellationToken ct = default)
    {
        var result = await _docker.GetOverviewAsync(hours, ct);
        return Ok(result);
    }

    /// <summary>Tail de log de um container monitorado (por alias).</summary>
    [HttpGet("logs")]
    public async Task<IActionResult> Logs(
        [FromQuery] string container,
        [FromQuery] int tail = 300,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(container))
            return BadRequest(new { message = "Parâmetro 'container' é obrigatório." });

        var result = await _docker.GetLogsAsync(container, tail, ct);
        return Ok(result);
    }
}
