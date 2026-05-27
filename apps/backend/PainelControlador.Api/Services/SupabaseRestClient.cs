using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PainelControlador.Api.Services;

public class SupabaseRestClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public SupabaseRestClient(HttpClient http) => _http = http;

    public async Task<List<T>> GetAsync<T>(string table, string? query = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(query) ? table : $"{table}?{query}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<T>>(json, _opts) ?? [];
    }

    public async Task PatchAsync(string table, string filter, object body, CancellationToken ct = default)
    {
        var url = $"{table}?{filter}";
        var json = JsonSerializer.Serialize(body, _opts);
        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        request.Headers.Add("Prefer", "return=minimal");
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> CountAsync(string table, string? filter = null, CancellationToken ct = default)
    {
        // limit=1 + Prefer: count=exact → PostgREST expõe Content-Range: 0-0/total sem trazer todos os dados.
        var qs = string.IsNullOrWhiteSpace(filter) ? "limit=1" : $"limit=1&{filter}";
        var url = $"{table}?{qs}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Prefer", "count=exact");
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        if (response.Headers.TryGetValues("Content-Range", out var values))
        {
            var range = values.FirstOrDefault() ?? "";
            var total = range.Split('/').LastOrDefault();
            return int.TryParse(total, out var count) ? count : 0;
        }
        return 0;
    }
}
