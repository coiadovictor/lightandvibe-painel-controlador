using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public class SqlConsultaException(string message) : Exception(message);

public interface ISqlService
{
    Task<SqlResultado> ConsultarAsync(string ambiente, string sql, CancellationToken ct);
}

/// <summary>
/// Console SQL: envia a consulta para a função consultar_sql do Supabase (só leitura) no ambiente escolhido.
/// </summary>
public class SqlService(IHttpClientFactory factory) : ISqlService
{
    public const int LimiteLinhas = 1000;

    public async Task<SqlResultado> ConsultarAsync(string ambiente, string sql, CancellationToken ct)
    {
        var client = factory.CreateClient(ambiente == "homolog" ? "supabase-homolog" : "supabase-producao");
        var corpo = new StringContent(JsonSerializer.Serialize(new { p_sql = sql }), Encoding.UTF8, "application/json");
        var relogio = Stopwatch.StartNew();
        HttpResponseMessage resposta;
        try
        {
            resposta = await client.PostAsync("rpc/consultar_sql", corpo, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new SqlConsultaException("Banco não respondeu em 20 segundos.");
        }
        catch (HttpRequestException ex)
        {
            throw new SqlConsultaException(ex.Message);
        }
        var texto = await resposta.Content.ReadAsStringAsync(ct);
        if (!resposta.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new SqlConsultaException($"Supabase respondeu {(int)resposta.StatusCode} sem corpo.");
            throw new SqlConsultaException(ExtrairMensagemErro(texto));
        }
        try
        {
            return MontarResultado(texto, relogio.ElapsedMilliseconds);
        }
        catch (JsonException)
        {
            throw new SqlConsultaException("Resposta do Supabase não é JSON: " + (texto.Length > 200 ? texto[..200] : texto));
        }
    }

    public static SqlResultado MontarResultado(string json, long duracaoMs)
    {
        using var doc = JsonDocument.Parse(json);
        var todas = doc.RootElement.EnumerateArray().ToList();
        var colunas = todas.Count == 0
            ? []
            : todas[0].EnumerateObject().Select(p => p.Name).ToList();
        var linhas = todas.Take(LimiteLinhas).Select(e => e.EnumerateObject().Select(p => p.Value.Clone()).ToList()).ToList();
        return new SqlResultado(colunas, linhas, todas.Count > LimiteLinhas, duracaoMs);
    }

    public static string ExtrairMensagemErro(string corpo)
    {
        try
        {
            using var doc = JsonDocument.Parse(corpo);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
            {
                var texto = msg.GetString() ?? "";
                if (doc.RootElement.TryGetProperty("hint", out var hint) && hint.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(hint.GetString()))
                    texto += $" ({hint.GetString()})";
                return texto;
            }
        }
        catch (JsonException) { }
        return corpo.Length > 500 ? corpo[..500] : corpo;
    }
}
