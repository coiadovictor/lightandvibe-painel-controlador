using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Services;
using Xunit;

namespace PainelControlador.Tests;

public class TrocaNumeroServiceTests
{
    private static (TrocaNumeroService svc, FakeHttpHandler supabase, FakeHttpHandler n8n) Montar(string? token = "tok")
    {
        var supabase = new FakeHttpHandler();
        var n8n = new FakeHttpHandler();
        var rest = new SupabaseRestClient(new HttpClient(supabase) { BaseAddress = new Uri("https://sb.local/rest/v1/") });
        var opts = new TrocaNumeroOptions { WebhookUrl = "https://n8n.local/webhook/x-troca-autorizada", Token = token };
        var svc = new TrocaNumeroService(rest, new HttpClient(n8n), opts, NullLogger<TrocaNumeroService>.Instance);
        return (svc, supabase, n8n);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private const string Troca5 = """[{"id":5,"matricula":"1","celular_antigo":"antigo@s.whatsapp.net","celular_novo":"novo@s.whatsapp.net","criado_em":"2026-10-19T10:00:00+00:00"}]""";

    [Fact]
    public async Task ListarPendentes_ExpiraAntesEJuntaNome_SemNomeNaoQuebra()
    {
        var (svc, sb, _) = Montar();
        sb.Responder = req => req.RequestUri!.AbsolutePath switch
        {
            var p when p.EndsWith("rpc/expirar_trocas_numero") => Json("0"),
            var p when p.EndsWith("acesso_troca_numero") => Json("""[{"id":1,"matricula":"10000666","celular_antigo":"551a@s.whatsapp.net","celular_novo":"551b@s.whatsapp.net","criado_em":"2026-10-19T10:00:00+00:00"},{"id":2,"matricula":"999","celular_antigo":"c","celular_novo":"d","criado_em":"2026-10-19T11:00:00+00:00"}]"""),
            var p when p.EndsWith("view_func_atendimento") => Json("""[{"funcionario":"10000666","nome":"Beatriz Cardoso"}]"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };

        var lista = await svc.ListarPendentesAsync();

        Assert.EndsWith("rpc/expirar_trocas_numero", sb.Requests[0].RequestUri!.AbsolutePath);
        Assert.Equal(2, lista.Count);
        Assert.Equal("Beatriz Cardoso", lista[0].Nome);
        Assert.Null(lista[1].Nome);
    }

    [Fact]
    public async Task Autorizar_ChamaRpcEWebhookComToken()
    {
        var (svc, sb, n8n) = Montar();
        n8n.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK);
        sb.Responder = req => req.RequestUri!.AbsolutePath.EndsWith("rpc/autorizar_troca_numero") ? Json("\"ok\"") : Json(Troca5);

        var r = await svc.AutorizarAsync(5, "rh.ana");

        Assert.True(r.Ok);
        Assert.True(r.SessaoAntigaInvalidada);
        Assert.Contains("\"p_usuario\":\"rh.ana\"", sb.Bodies.First(b => b.Contains("p_id")));
        Assert.Equal("tok", n8n.Requests[0].Headers.GetValues("x-troca-token").First());
        Assert.Contains("antigo@s.whatsapp.net", n8n.Bodies[0]);
    }

    [Fact]
    public async Task Autorizar_Invalida_NaoChamaWebhook()
    {
        var (svc, sb, n8n) = Montar();
        sb.Responder = req => req.RequestUri!.AbsolutePath.EndsWith("rpc/autorizar_troca_numero") ? Json("\"invalida\"") : Json("[]");

        var r = await svc.AutorizarAsync(5, "rh.ana");

        Assert.False(r.Ok);
        Assert.Empty(n8n.Requests);
    }

    [Fact]
    public async Task Autorizar_WebhookForaDoAr_AindaDevolveOk()
    {
        var (svc, sb, n8n) = Montar();
        sb.Responder = req => req.RequestUri!.AbsolutePath.EndsWith("rpc/autorizar_troca_numero") ? Json("\"ok\"") : Json(Troca5);
        n8n.Responder = _ => throw new HttpRequestException("down");

        var r = await svc.AutorizarAsync(5, "rh.ana");

        Assert.True(r.Ok);
        Assert.False(r.SessaoAntigaInvalidada);
    }

    [Fact]
    public async Task Autorizar_SemToken_NaoChamaWebhook()
    {
        var (svc, sb, n8n) = Montar(token: null);
        sb.Responder = req => req.RequestUri!.AbsolutePath.EndsWith("rpc/autorizar_troca_numero") ? Json("\"ok\"") : Json(Troca5);

        var r = await svc.AutorizarAsync(5, "rh.ana");

        Assert.True(r.Ok);
        Assert.False(r.SessaoAntigaInvalidada);
        Assert.Empty(n8n.Requests);
    }

    [Fact]
    public async Task Recusar_ChamaRpc()
    {
        var (svc, sb, n8n) = Montar();
        sb.Responder = _ => Json("\"ok\"");

        var r = await svc.RecusarAsync(9, "rh.ana");

        Assert.True(r.Ok);
        Assert.EndsWith("rpc/recusar_troca_numero", sb.Requests[0].RequestUri!.AbsolutePath);
        Assert.Empty(n8n.Requests);
    }
}
