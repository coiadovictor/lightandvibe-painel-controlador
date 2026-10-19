using System.Net;
using PainelControlador.Api.Services;
using Xunit;

namespace PainelControlador.Tests;

public class SupabaseRestClientTests
{
    [Fact]
    public async Task RpcAsync_PostaNaRotaRpcEDevolveTextoSemAspas()
    {
        var handler = new FakeHttpHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("\"ok\"") }
        };
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://supabase.local/rest/v1/") };
        var client = new SupabaseRestClient(http);

        var resultado = await client.RpcAsync("autorizar_troca_numero", new { p_id = 7, p_usuario = "rh.ana" });

        Assert.Equal("ok", resultado);
        Assert.Equal("https://supabase.local/rest/v1/rpc/autorizar_troca_numero", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Contains("\"p_id\":7", handler.Bodies[0]);
    }
}
