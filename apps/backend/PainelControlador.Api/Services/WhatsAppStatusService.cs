using System.Text.Json;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public record WhatsAppStatusResult(
    bool Available,
    string? Message,
    IReadOnlyList<WhatsAppInstanceDto> Instances);

public interface IWhatsAppStatusService
{
    Task<WhatsAppStatusResult> GetStatusAsync(CancellationToken ct);
}

/// <summary>
/// Consulta o estado atual (autoritativo) das instâncias do WhatsApp na Evolution API,
/// via GET /instance/fetchInstances. Diferente da detecção por log, isto reflete o
/// estado AGORA (liga/desliga o alerta sozinho conforme conecta/desconecta).
/// Somente leitura.
/// </summary>
public class WhatsAppStatusService : IWhatsAppStatusService
{
    private readonly EvolutionOptions _opts;
    private readonly HttpClient _http;
    private readonly ILogger<WhatsAppStatusService> _logger;

    public WhatsAppStatusService(EvolutionOptions opts, HttpClient http, ILogger<WhatsAppStatusService> logger)
    {
        _opts = opts;
        _http = http;
        _logger = logger;
    }

    public async Task<WhatsAppStatusResult> GetStatusAsync(CancellationToken ct)
    {
        if (!_opts.IsConfigured)
            return new WhatsAppStatusResult(false,
                "Checagem do WhatsApp não configurada (defina EVOLUTION_API_URL e EVOLUTION_API_KEY).",
                Array.Empty<WhatsAppInstanceDto>());

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_opts.BaseUrl}/instance/fetchInstances");
            req.Headers.Add("apikey", _opts.ApiKey);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return new WhatsAppStatusResult(false,
                    $"Evolution respondeu {(int)resp.StatusCode}.",
                    Array.Empty<WhatsAppInstanceDto>());

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var instances = ParseInstances(doc.RootElement);
            return new WhatsAppStatusResult(true, null, instances);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar instâncias do WhatsApp na Evolution");
            return new WhatsAppStatusResult(false,
                $"Não foi possível consultar a Evolution: {ex.Message}",
                Array.Empty<WhatsAppInstanceDto>());
        }
    }

    private static IReadOnlyList<WhatsAppInstanceDto> ParseInstances(JsonElement root)
    {
        // fetchInstances retorna um array de instâncias (formato Evolution v2).
        // Toleramos também { "instances": [...] } por segurança.
        var arr = root.ValueKind == JsonValueKind.Array
            ? root
            : (root.TryGetProperty("instances", out var inner) && inner.ValueKind == JsonValueKind.Array ? inner : default);

        var list = new List<WhatsAppInstanceDto>();
        if (arr.ValueKind != JsonValueKind.Array) return list;

        foreach (var el in arr.EnumerateArray())
        {
            var name = Str(el, "name") ?? Str(el, "instanceName") ?? "(sem nome)";
            var state = Str(el, "connectionStatus") ?? Str(el, "state") ?? "unknown";
            var profile = Str(el, "profileName");
            var ownerJid = Str(el, "ownerJid");
            var number = string.IsNullOrEmpty(ownerJid) ? null : ownerJid.Split('@')[0];

            DateTime? disconnectedAt = null;
            if (DateTime.TryParse(Str(el, "disconnectionAt"), null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var dt))
                disconnectedAt = dt;

            list.Add(new WhatsAppInstanceDto(
                name, state,
                string.Equals(state, "open", StringComparison.OrdinalIgnoreCase),
                profile, number, disconnectedAt));
        }
        return list;
    }

    private static string? Str(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
