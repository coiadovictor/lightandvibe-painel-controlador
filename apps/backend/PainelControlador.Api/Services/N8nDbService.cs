using Npgsql;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface IN8nDbService
{
    Task<LogStatsDto> GetLogStatsAsync(CancellationToken ct);
    Task<List<LogEntryN8nDto>> GetRecentMessagesAsync(int limit, CancellationToken ct, string? sessionId = null);
}

public class N8nDbService : IN8nDbService
{
    private readonly N8nOptions _opts;
    private readonly IEmployeeService _employees;
    private readonly ILogger<N8nDbService> _logger;

    public N8nDbService(N8nOptions opts, IEmployeeService employees, ILogger<N8nDbService> logger)
    {
        _opts = opts;
        _employees = employees;
        _logger = logger;
    }

    public async Task<LogStatsDto> GetLogStatsAsync(CancellationToken ct)
    {
        var empty = new LogStatsDto(0, 0, 0, 0, 0, 0, 0, []);
        if (!_opts.IsConfigured) return empty;

        try
        {
            await using var conn = new NpgsqlConnection(NormalizeCs(_opts.ConnectionString));
            await conn.OpenAsync(ct);

            long total = 0, sessoes = 0, hoje = 0, ultimaHora = 0, humanas = 0, ia = 0;
            var rawSessoes = new List<(string SessionId, int Total, DateTime? Ultima)>();

            // Exclui tipo 'tool' de todas as contagens
            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM n8n_chat_histories WHERE message->>'type' != 'tool'", conn))
                total = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(DISTINCT session_id) FROM n8n_chat_histories WHERE message->>'type' != 'tool'", conn))
                sessoes = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM n8n_chat_histories WHERE message->>'type' != 'tool' AND data >= CURRENT_DATE", conn))
                hoje = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM n8n_chat_histories WHERE message->>'type' != 'tool' AND data >= NOW() - INTERVAL '1 hour'", conn))
                ultimaHora = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            // Formato n8n: { "type": "human"|"ai", "content": "...", ... }
            await using (var cmd = new NpgsqlCommand(@"
                SELECT
                    COUNT(*) FILTER (WHERE message->>'type' = 'human') AS humanas,
                    COUNT(*) FILTER (WHERE message->>'type' IN ('ai', 'assistant')) AS ia
                FROM n8n_chat_histories
                WHERE message->>'type' != 'tool'", conn))
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                if (await reader.ReadAsync(ct))
                {
                    humanas = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
                    ia      = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
                }
            }

            // Sessões ordenadas pela mais recente, excluindo tool
            await using (var cmd = new NpgsqlCommand(@"
                SELECT session_id,
                       COUNT(*) AS total,
                       MAX(data) AS ultima
                FROM n8n_chat_histories
                WHERE message->>'type' != 'tool'
                GROUP BY session_id
                ORDER BY MAX(data) DESC
                LIMIT 10", conn))
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    var sid = reader.GetString(0);
                    var tot = (int)reader.GetInt64(1);
                    var ult = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
                    rawSessoes.Add((sid, tot, ult));
                }
            }

            var phoneMap = await BuildPhoneMapAsync(ct);

            var ultimasSessoes = rawSessoes.Select(s =>
            {
                var (tel, nome) = ResolveContact(s.SessionId, phoneMap);
                return new SessaoResumoDto(s.SessionId, s.Total, s.Ultima, tel, nome);
            }).ToList();

            var media = sessoes > 0 ? Math.Round((double)total / sessoes, 1) : 0;
            return new LogStatsDto(total, sessoes, hoje, ultimaHora, humanas, ia, media, ultimasSessoes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar estatísticas do banco n8n.");
            return empty;
        }
    }

    public async Task<List<LogEntryN8nDto>> GetRecentMessagesAsync(int limit = 100, CancellationToken ct = default, string? sessionId = null)
    {
        if (!_opts.IsConfigured) return [];

        try
        {
            await using var conn = new NpgsqlConnection(NormalizeCs(_opts.ConnectionString));
            await conn.OpenAsync(ct);

            var rawRows = new List<(string Id, string SessionId, string Tipo, string Conteudo, DateTime? Data)>();

            var sql = sessionId is null
                ? @"SELECT id::text, session_id,
                           COALESCE(message->>'type', 'unknown') AS tipo,
                           COALESCE(message->>'content', '') AS conteudo,
                           data
                    FROM n8n_chat_histories
                    WHERE message->>'type' != 'tool'
                    ORDER BY data DESC
                    LIMIT @limit"
                : @"SELECT id::text, session_id,
                           COALESCE(message->>'type', 'unknown') AS tipo,
                           COALESCE(message->>'content', '') AS conteudo,
                           data
                    FROM n8n_chat_histories
                    WHERE session_id = @sessionId
                      AND message->>'type' != 'tool'
                    ORDER BY data ASC
                    LIMIT @limit";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("limit", limit);
            if (sessionId is not null)
                cmd.Parameters.AddWithValue("sessionId", sessionId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                rawRows.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetDateTime(4)
                ));
            }

            var phoneMap = await BuildPhoneMapAsync(ct);

            return rawRows.Select(r =>
            {
                var (_, nome) = ResolveContact(r.SessionId, phoneMap);
                return new LogEntryN8nDto(r.Id, r.SessionId, r.Tipo, r.Conteudo, r.Data, nome);
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar mensagens do banco n8n.");
            return [];
        }
    }

    // Converte URI postgres:// para formato key=value aceito pelo Npgsql
    private static string NormalizeCs(string cs)
    {
        if (!cs.StartsWith("postgres://") && !cs.StartsWith("postgresql://"))
            return cs;

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

    // Monta dicionário: últimos 11 dígitos do celular → nome do funcionário
    private async Task<Dictionary<string, string>> BuildPhoneMapAsync(CancellationToken ct)
    {
        try
        {
            var lista = await _employees.ListAsync(ct);
            return lista
                .Where(e => !string.IsNullOrWhiteSpace(e.Phone) && !string.IsNullOrWhiteSpace(e.Name))
                .Select(e => (Digits: OnlyDigits(e.Phone!), Name: e.Name))
                .Where(x => x.Digits.Length >= 8)
                .DistinctBy(x => x.Digits[^11..])
                .ToDictionary(
                    x => x.Digits.Length >= 11 ? x.Digits[^11..] : x.Digits,
                    x => x.Name
                );
        }
        catch
        {
            return [];
        }
    }

    // Extrai telefone e nome a partir do session_id
    // session_id formato WhatsApp n8n: "5511987654321" ou "5511987654321@s.whatsapp.net" ou variações
    private static (string? Telefone, string? NomeFuncionario) ResolveContact(
        string sessionId, Dictionary<string, string> phoneMap)
    {
        var digits = OnlyDigits(sessionId);
        if (digits.Length < 8) return (null, null);

        // Tenta match pelo sufixo de 11 dígitos (DDD + número BR) ou 8 (número sem DDD)
        foreach (var len in new[] { 11, 10, 8 })
        {
            if (digits.Length < len) continue;
            var suffix = digits[^len..];
            if (phoneMap.TryGetValue(suffix, out var nome))
            {
                var tel = FormatPhone(digits);
                return (tel, nome);
            }
        }

        // Nenhum match — retorna só o telefone formatado
        var formattedTel = digits.Length >= 10 ? FormatPhone(digits) : null;
        return (formattedTel, null);
    }

    private static string OnlyDigits(string s)
        => new(s.Where(char.IsDigit).ToArray());

    // Formata número brasileiro: 55 (11) 9 8765-4321
    private static string FormatPhone(string digits)
    {
        // Remove DDI 55 se presente
        if (digits.Length == 13 && digits.StartsWith("55"))
            digits = digits[2..];
        return digits.Length == 11
            ? $"({digits[..2]}) {digits[2..7]}-{digits[7..]}"
            : digits.Length == 10
                ? $"({digits[..2]}) {digits[2..6]}-{digits[6..]}"
                : digits;
    }
}
