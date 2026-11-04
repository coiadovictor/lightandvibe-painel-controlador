using System.Net;
using System.Text.Json;
using PainelControlador.Api.Services;
using Xunit;

namespace PainelControlador.Tests;

public class SqlServiceTests
{
    [Fact]
    public void MontarResultado_ColunasNaOrdemDaPrimeiraLinha()
    {
        var r = SqlService.MontarResultado("[{\"b\":1,\"a\":\"x\"},{\"b\":2,\"a\":null}]", 12);

        Assert.Equal(new[] { "b", "a" }, r.Colunas);
        Assert.Equal(2, r.Linhas.Count);
        Assert.False(r.Truncado);
        Assert.Equal(12, r.DuracaoMs);
    }

    [Fact]
    public void MontarResultado_SemLinhasDevolveColunasVazias()
    {
        var r = SqlService.MontarResultado("[]", 1);

        Assert.Empty(r.Colunas);
        Assert.Empty(r.Linhas);
        Assert.False(r.Truncado);
    }

    [Fact]
    public void MontarResultado_1001LinhasMarcaTruncadoEDevolve1000()
    {
        var json = "[" + string.Join(",", Enumerable.Range(1, 1001).Select(i => $"{{\"n\":{i}}}")) + "]";

        var r = SqlService.MontarResultado(json, 5);

        Assert.Equal(1000, r.Linhas.Count);
        Assert.True(r.Truncado);
    }

    [Fact]
    public void MontarResultado_ColunasRepetidasMantemOrdemSemQuebrar()
    {
        var r = SqlService.MontarResultado("[{\"?column?\":1,\"?column?\":2}]", 1);

        Assert.Equal(new[] { "?column?", "?column?" }, r.Colunas);
        Assert.Single(r.Linhas);
        Assert.Equal(2, r.Linhas[0].Count);
        Assert.Equal(1, r.Linhas[0][0].GetInt32());
        Assert.Equal(2, r.Linhas[0][1].GetInt32());
    }

    [Fact]
    public void MontarResultado_LinhaViraListaDeValoresNaOrdemDasColunas()
    {
        var r = SqlService.MontarResultado("[{\"b\":1,\"a\":\"x\"}]", 1);

        Assert.Equal(1, r.Linhas[0][0].GetInt32());
        Assert.Equal("x", r.Linhas[0][1].GetString());
    }

    [Fact]
    public void ExtrairMensagemErro_MessageQueNaoEhStringNaoQuebra()
    {
        Assert.Equal("{\"message\":5}", SqlService.ExtrairMensagemErro("{\"message\":5}"));
    }

    [Fact]
    public async Task ConsultarAsync_5xxSemCorpoViraMensagemLegivel()
    {
        var handler = new FakeHttpHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.BadGateway) };
        var svc = new SqlService(new FactoryFake { ["supabase-producao"] = new HttpClient(handler) { BaseAddress = new Uri("https://prod.local/rest/v1/") } });

        var ex = await Assert.ThrowsAsync<SqlConsultaException>(() => svc.ConsultarAsync("producao", "select 1", CancellationToken.None));

        Assert.Contains("502", ex.Message);
    }

    [Fact]
    public async Task ConsultarAsync_200ComCorpoNaoJsonViraSqlConsultaException()
    {
        var handler = new FakeHttpHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>proxy</html>") } };
        var svc = new SqlService(new FactoryFake { ["supabase-producao"] = new HttpClient(handler) { BaseAddress = new Uri("https://prod.local/rest/v1/") } });

        await Assert.ThrowsAsync<SqlConsultaException>(() => svc.ConsultarAsync("producao", "select 1", CancellationToken.None));
    }

    [Fact]
    public async Task ConsultarAsync_FalhaDeRedeViraSqlConsultaException()
    {
        var handler = new FakeHttpHandler { Responder = _ => throw new HttpRequestException("conexão recusada") };
        var svc = new SqlService(new FactoryFake { ["supabase-producao"] = new HttpClient(handler) { BaseAddress = new Uri("https://prod.local/rest/v1/") } });

        var ex = await Assert.ThrowsAsync<SqlConsultaException>(() => svc.ConsultarAsync("producao", "select 1", CancellationToken.None));

        Assert.Equal("conexão recusada", ex.Message);
    }

    [Fact]
    public void ExtrairMensagemErro_UsaMessageEHintDoPostgrest()
    {
        var corpo = "{\"code\":\"42P01\",\"message\":\"relation \\\"x\\\" does not exist\",\"details\":null,\"hint\":\"Perhaps you meant \\\"y\\\"\"}";

        Assert.Equal("relation \"x\" does not exist (Perhaps you meant \"y\")", SqlService.ExtrairMensagemErro(corpo));
    }

    [Fact]
    public void ExtrairMensagemErro_CorpoNaoJsonDevolveTextoBruto()
    {
        Assert.Equal("<html>502</html>", SqlService.ExtrairMensagemErro("<html>502</html>"));
    }

    [Fact]
    public async Task ConsultarAsync_PostaNoRpcDoAmbienteEscolhido()
    {
        var handler = new FakeHttpHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[{\"n\":1}]") }
        };
        var factory = new FactoryFake
        {
            ["supabase-homolog"] = new HttpClient(handler) { BaseAddress = new Uri("https://hml.local/rest/v1/") },
            ["supabase-producao"] = new HttpClient(new FakeHttpHandler()) { BaseAddress = new Uri("https://prod.local/rest/v1/") },
        };
        var svc = new SqlService(factory);

        var r = await svc.ConsultarAsync("homolog", "select 1", CancellationToken.None);

        Assert.Equal("https://hml.local/rest/v1/rpc/consultar_sql", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Contains("\"p_sql\":\"select 1\"", handler.Bodies[0]);
        Assert.Equal(new[] { "n" }, r.Colunas);
    }

    [Fact]
    public async Task ConsultarAsync_ErroDoPostgrestViraSqlConsultaException()
    {
        var handler = new FakeHttpHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"message\":\"Só SELECT é permitido.\"}") }
        };
        var factory = new FactoryFake { ["supabase-producao"] = new HttpClient(handler) { BaseAddress = new Uri("https://prod.local/rest/v1/") } };
        var svc = new SqlService(factory);

        var ex = await Assert.ThrowsAsync<SqlConsultaException>(() => svc.ConsultarAsync("producao", "update x set a = 1", CancellationToken.None));

        Assert.Equal("Só SELECT é permitido.", ex.Message);
    }

    private sealed class FactoryFake : Dictionary<string, HttpClient>, IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => this[name];
    }
}
