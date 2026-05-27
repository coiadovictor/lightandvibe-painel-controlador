using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogService _service;
    private readonly IN8nDbService _n8n;
    private readonly N8nOptions _n8nOpts;

    public LogsController(ILogService service, IN8nDbService n8n, N8nOptions n8nOpts)
    {
        _service = service;
        _n8n = n8n;
        _n8nOpts = n8nOpts;
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

    // Diagnóstico — retorna status da conexão n8n sem expor a senha
    [HttpGet("diagnostics")]
    public async Task<IActionResult> Diagnostics(CancellationToken ct)
    {
        var isConfigured = _n8nOpts.IsConfigured;
        var csMasked = isConfigured
            ? System.Text.RegularExpressions.Regex.Replace(
                _n8nOpts.ConnectionString, @"(?<=:)[^:@]+(?=@)", "***")
            : "(not set)";

        string connStatus;
        string? connError = null;
        long rowCount = 0;

        if (!isConfigured)
        {
            connStatus = "not_configured";
        }
        else
        {
            try
            {
                await using var conn = new NpgsqlConnection(_n8nOpts.ConnectionString);
                await conn.OpenAsync(ct);
                await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM n8n_chat_histories", conn);
                rowCount = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);
                connStatus = "ok";
            }
            catch (Exception ex)
            {
                connStatus = "error";
                connError = ex.Message;
            }
        }

        return Ok(new
        {
            isConfigured,
            connectionString = csMasked,
            connectionStatus = connStatus,
            error = connError,
            rowCount,
        });
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var logs = await _service.ListAsync(ct);
        return Ok(logs);
    }
}
