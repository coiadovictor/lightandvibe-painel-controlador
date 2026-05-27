using System.Text.Json;
using System.Text.Json.Serialization;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public class LogService : ILogService
{
    private readonly SupabaseRestClient _sb;
    private readonly ILogger<LogService> _logger;

    public LogService(SupabaseRestClient sb, ILogger<LogService> logger)
    {
        _sb = sb;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LogEntryDto>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            var rows = await _sb.GetAsync<EventoRow>(
                "eventos_conversa",
                "order=criado_em.desc&limit=200",
                ct);

            return rows.Select(r => new LogEntryDto(
                Id: r.Id,
                Level: r.Status == "sucesso" ? "INFO" : "ERROR",
                Message: r.TipoEvento ?? "—",
                Source: r.ConversaId != null ? $"conversa:{r.ConversaId[..8]}…" : null,
                CreatedAt: r.CriadoEm
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao listar eventos_conversa.");
            return [];
        }
    }

    private record EventoRow(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("conversa_id")] string? ConversaId,
        [property: JsonPropertyName("tipo_evento")] string? TipoEvento,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("latencia_ms")] int? LatenciaMs,
        [property: JsonPropertyName("criado_em")] DateTime CriadoEm
    );
}
