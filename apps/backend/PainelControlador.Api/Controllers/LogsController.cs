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
    public async Task<IActionResult> GetMessages([FromQuery] int limit = 100, [FromQuery] string? sessionId = null, CancellationToken ct = default)
    {
        if (limit > 500) limit = 500;
        var messages = await _n8n.GetRecentMessagesAsync(limit, ct, sessionId);
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
                var normalizedCs = System.Text.RegularExpressions.Regex.IsMatch(_n8nOpts.ConnectionString, @"^postgres(ql)?://")
                    ? NormalizeUri(_n8nOpts.ConnectionString)
                    : _n8nOpts.ConnectionString;
                await using var conn = new NpgsqlConnection(normalizedCs);
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

    private static string NormalizeUri(string cs)
    {
        var uri = new Uri(cs);
        var userParts = uri.UserInfo.Split(':', 2);
        var user = userParts[0];
        var pass = userParts.Length > 1 ? Uri.UnescapeDataString(userParts[1]) : "";
        var db   = uri.AbsolutePath.TrimStart('/');
        var sslMode = "Prefer";
        foreach (var param in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = param.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "sslmode")
                sslMode = kv[1] switch { "disable" => "Disable", "require" => "Require", _ => "Prefer" };
        }
        return $"Host={uri.Host};Port={uri.Port};Database={db};Username={user};Password={pass};SSL Mode={sslMode};Trust Server Certificate=true";
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var logs = await _service.ListAsync(ct);
        return Ok(logs);
    }
}
