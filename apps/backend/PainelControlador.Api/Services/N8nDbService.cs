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
            await using var conn = new NpgsqlConnection(_opts.ConnectionString);
            await conn.OpenAsync(ct);

            long total = 0, sessoes = 0, hoje = 0, ultimaHora = 0, humanas = 0, ia = 0;
            var rawSessoes = new List<(string SessionId, int Total, DateTime? Ultima)>();

            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM n8n_chat_histories", conn))
                total = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(DISTINCT session_id) FROM n8n_chat_histories", conn))
                sessoes = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

            await using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM n8n_chat_histories WHERE created_at >= CURRENT_DATE", conn))
                hoje = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

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

    public async Task<List<LogEntryN8nDto>> GetRecentMessagesAsync(int limit = 100, CancellationToken ct = default)
    {
        if (!_opts.IsConfigured) return [];

        try
        {
            await using var conn = new NpgsqlConnection(_opts.ConnectionString);
            await conn.OpenAsync(ct);

            var rawRows = new List<(string Id, string SessionId, string Tipo, string Conteudo, DateTime CriadoEm)>();

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
                rawRows.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetDateTime(4)
                ));
            }

            var phoneMap = await BuildPhoneMapAsync(ct);

            return rawRows.Select(r =>
            {
                var (_, nome) = ResolveContact(r.SessionId, phoneMap);
                return new LogEntryN8nDto(r.Id, r.SessionId, r.Tipo, r.Conteudo, r.CriadoEm, nome);
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar mensagens do banco n8n.");
            return [];
        }
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
