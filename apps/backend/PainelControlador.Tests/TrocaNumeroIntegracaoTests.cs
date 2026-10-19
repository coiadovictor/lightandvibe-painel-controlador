using Microsoft.Extensions.Logging.Abstractions;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Services;
using Xunit;

namespace PainelControlador.Tests;

public class TrocaNumeroIntegracaoTests
{
    private static string? Env(string nome) => Environment.GetEnvironmentVariable(nome);

    [Fact]
    public async Task AutorizarPendenteEmHomolog_MigraVinculoEInvalidaSessao()
    {
        var url = Env("INTEGRACAO_SUPABASE_URL"); var key = Env("INTEGRACAO_SUPABASE_KEY");
        var webhook = Env("INTEGRACAO_WEBHOOK_URL"); var token = Env("INTEGRACAO_WEBHOOK_TOKEN");
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(webhook) || string.IsNullOrEmpty(token))
            return;

        var http = new HttpClient { BaseAddress = new Uri(url.TrimEnd('/') + "/rest/v1/") };
        http.DefaultRequestHeaders.Add("apikey", key);
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
        var svc = new TrocaNumeroService(new SupabaseRestClient(http), new HttpClient(),
            new TrocaNumeroOptions { WebhookUrl = webhook, Token = token }, NullLogger<TrocaNumeroService>.Instance);

        var pendentes = await svc.ListarPendentesAsync();
        Assert.NotEmpty(pendentes);
        var alvo = pendentes[0];

        var r = await svc.AutorizarAsync(alvo.Id, "teste.integracao");

        Assert.True(r.Ok);
        Assert.True(r.SessaoAntigaInvalidada);
        var depois = await svc.ListarPendentesAsync();
        Assert.DoesNotContain(depois, t => t.Id == alvo.Id);
        var repetido = await svc.AutorizarAsync(alvo.Id, "teste.integracao");
        Assert.False(repetido.Ok);
    }
}
