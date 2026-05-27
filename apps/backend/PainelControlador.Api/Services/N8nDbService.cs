using Npgsql;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface IN8nDbService
{
    Task<LogStatsDto> GetLogStatsAsync(CancellationToken ct);
    Task<List<LogEntryN8nDto>> GetRecentMessagesAsync(int limit, CancellationToken ct);
}

public class N8nDbService : IN8nDbService
{
    private readonly N8nOptions _opts;
    private readonly ILogger<N8nDbService> _logger;

    public N8nDbService(N8nOptions opts, ILogger<N8nDbService> logger)
    {
        _opts = opts;
        _logger = logger;
    }

    public async Task<LogStatsDto> GetLogStatsAsync(CancellationToken ct)
    {
        var empty = new LogStatsDto(0, 0, 0, 0, 0, 0, 0, []);
        if (!_opts.IsConfigured) return empty;

        try
        {
            await using var conn = new NpgsqlConnection(_opts.ConnectionString);
            await conn.OpenAsync(ct);

            long total = 0, sessoes = 0, hoje = 0, ultimaHora = 0, humanas = 0, ia = 0;
            var ultimasSessoes = new List<SessaoResumoDto>();

            // Total de mensagens
            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM n8n_chat_histories", conn))
                total = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            // Sessões únicas
            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(DISTINCT session_id) FROM n8n_chat_histories", conn))
                sessoes = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            // Mensagens hoje
            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM n8n_chat_histories WHERE created_at >= CURRENT_DATE", conn))
                hoje = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            // Mensagens última hora
            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM n8n_chat_histories WHERE created_at >= NOW() - INTERVAL '1 hour'", conn))
                ultimaHora = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            // Formato n8n: { "type": "human"|"ai", "content": "...", ... }
            await using (var cmd = new NpgsqlCommand(@"
                SELECT
                    COUNT(*) FILTER (WHERE message->>'type' = 'human') AS humanas,
                    COUNT(*) FILTER (WHERE message->>'type' IN ('ai', 'assistant')) AS ia
                FROM n8n_chat_histories", conn))
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                if (await reader.ReadAsync(ct))
                {
                    humanas = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
                    ia      = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
                }
            }

            // Últimas 10 sessões com contagem e última mensagem
            await using (var cmd = new NpgsqlCommand(@"
                SELECT session_id,
                       COUNT(*) AS total,
                       MAX(created_at) AS ultima
                FROM n8n_chat_histories
                GROUP BY session_id
                ORDER BY MAX(created_at) DESC
                LIMIT 10", conn))
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    var sid   = reader.GetString(0);
                    var tot   = (int)reader.GetInt64(1);
                    var ult   = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
                    ultimasSessoes.Add(new SessaoResumoDto(sid, tot, ult));
                }
            }

            var media = sessoes > 0 ? Math.Round((double)total / sessoes, 1) : 0;
            return new LogStatsDto(total, sessoes, hoje, ultimaHora, humanas, ia, media, ultimasSessoes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar estatísticas do banco n8n.");
            return empty;
        }
    }

    public async Task<List<LogEntryN8nDto>> GetRecentMessagesAsync(int limit = 100, CancellationToken ct = default)
    {
        if (!_opts.IsConfigured) return [];

        try
        {
            await using var conn = new NpgsqlConnection(_opts.ConnectionString);
            await conn.OpenAsync(ct);

            var result = new List<LogEntryN8nDto>();

            await using var cmd = new NpgsqlCommand(@"
                SELECT id::text,
                       session_id,
                       COALESCE(message->>'type', 'unknown') AS tipo,
                       COALESCE(message->>'content', '') AS conteudo,
                       created_at
                FROM n8n_chat_histories
                ORDER BY created_at DESC
                LIMIT @limit", conn);

            cmd.Parameters.AddWithValue("limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(new LogEntryN8nDto(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetDateTime(4)
                ));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar mensagens do banco n8n.");
            return [];
        }
    }
}
