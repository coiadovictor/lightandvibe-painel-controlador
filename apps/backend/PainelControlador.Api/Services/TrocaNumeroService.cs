using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface ITrocaNumeroService
{
    Task<List<TrocaNumeroDto>> ListarPendentesAsync(CancellationToken ct = default);
    Task<int> ContarPendentesAsync(CancellationToken ct = default);
    Task<DecisaoTrocaResultado> AutorizarAsync(long id, string usuario, CancellationToken ct = default);
    Task<DecisaoTrocaResultado> RecusarAsync(long id, string usuario, CancellationToken ct = default);
}

public class TrocaNumeroService : ITrocaNumeroService
{
    private readonly SupabaseRestClient _supabase;
    private readonly HttpClient _n8n;
    private readonly TrocaNumeroOptions _opts;
    private readonly ILogger<TrocaNumeroService> _log;

    public TrocaNumeroService(SupabaseRestClient supabase, HttpClient n8n, TrocaNumeroOptions opts, ILogger<TrocaNumeroService> log)
    {
        _supabase = supabase; _n8n = n8n; _opts = opts; _log = log;
    }

    private sealed class TrocaRow
    {
        public long Id { get; set; }
        public string Matricula { get; set; } = "";
        [JsonPropertyName("celular_antigo")] public string CelularAntigo { get; set; } = "";
        [JsonPropertyName("celular_novo")] public string CelularNovo { get; set; } = "";
        [JsonPropertyName("criado_em")] public DateTime CriadoEm { get; set; }
    }

    private sealed class AcessoRow
    {
        public string? Celular { get; set; }
    }

    private sealed class NomeRow
    {
        public string Funcionario { get; set; } = "";
        public string? Nome { get; set; }
    }

    public async Task<List<TrocaNumeroDto>> ListarPendentesAsync(CancellationToken ct = default)
    {
        await _supabase.RpcAsync("expirar_trocas_numero", new { }, ct);
        var rows = await _supabase.GetAsync<TrocaRow>("acesso_troca_numero", "status=eq.pendente&order=criado_em.asc", ct);
        if (rows.Count == 0) return [];
        var mats = string.Join(",", rows.Select(r => r.Matricula).Distinct());
        var nomes = await _supabase.GetAsync<NomeRow>("view_func_atendimento", $"select=funcionario,nome&funcionario=in.({mats})", ct);
        var porMatricula = nomes.GroupBy(n => n.Funcionario).ToDictionary(g => g.Key, g => g.First().Nome);
        return rows.Select(r => new TrocaNumeroDto(r.Id, r.Matricula, porMatricula.GetValueOrDefault(r.Matricula), r.CelularAntigo, r.CelularNovo, r.CriadoEm)).ToList();
    }

    public async Task<int> ContarPendentesAsync(CancellationToken ct = default)
        => await _supabase.CountAsync("acesso_troca_numero", "status=eq.pendente", ct);

    public async Task<DecisaoTrocaResultado> AutorizarAsync(long id, string usuario, CancellationToken ct = default)
    {
        var troca = (await _supabase.GetAsync<TrocaRow>("acesso_troca_numero", $"id=eq.{id}", ct)).FirstOrDefault();
        var celularVinculado = troca is null ? null
            : (await _supabase.GetAsync<AcessoRow>("pag_funcionario_acesso", $"select=celular&matricula=eq.{troca.Matricula}", ct)).FirstOrDefault()?.Celular;
        var r = await _supabase.RpcAsync("autorizar_troca_numero", new { p_id = id, p_usuario = usuario }, ct);
        if (r != "ok") return new DecisaoTrocaResultado(false, false);
        var celularAnterior = celularVinculado ?? troca?.CelularAntigo;
        var invalidada = celularAnterior is not null && await InvalidarSessaoAntigaAsync(celularAnterior, ct);
        return new DecisaoTrocaResultado(true, invalidada);
    }

    public async Task<DecisaoTrocaResultado> RecusarAsync(long id, string usuario, CancellationToken ct = default)
    {
        var r = await _supabase.RpcAsync("recusar_troca_numero", new { p_id = id, p_usuario = usuario }, ct);
        return new DecisaoTrocaResultado(r == "ok", false);
    }

    private async Task<bool> InvalidarSessaoAntigaAsync(string celularAntigo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opts.WebhookUrl) || string.IsNullOrWhiteSpace(_opts.Token))
        {
            _log.LogWarning("TrocaNumero sem WebhookUrl ou Token; sessão antiga não invalidada");
            return false;
        }
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, _opts.WebhookUrl);
            req.Headers.Add("x-troca-token", _opts.Token);
            req.Content = JsonContent.Create(new { celular_antigo = celularAntigo });
            using var resp = await _n8n.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                _log.LogWarning("Webhook troca-autorizada devolveu {Status}", (int)resp.StatusCode);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Falha ao invalidar sessão antiga de {Celular}", celularAntigo);
            return false;
        }
    }
}
