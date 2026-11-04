using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Controllers;
using PainelControlador.Api.Dtos;
using PainelControlador.Api.Services;
using Xunit;

namespace PainelControlador.Tests;

public class SqlControllerTests
{
    [Fact]
    public async Task Consultar_AmbienteInvalidoDevolve400()
    {
        var ctrl = new SqlController(new ServicoFake((_, _) => Resultado()));

        var r = await ctrl.Consultar(new SqlConsultaRequest("teste", "select 1"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(r);
    }

    [Fact]
    public async Task Consultar_SqlVazioDevolve400()
    {
        var ctrl = new SqlController(new ServicoFake((_, _) => Resultado()));

        var r = await ctrl.Consultar(new SqlConsultaRequest("producao", "   "), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(r);
    }

    [Fact]
    public async Task Consultar_ExcecaoDoServicoVira400ComMessage()
    {
        var ctrl = new SqlController(new ServicoFake((_, _) => throw new SqlConsultaException("x")));

        var r = await ctrl.Consultar(new SqlConsultaRequest("producao", "select 1"), CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(r);
        Assert.Contains("x", System.Text.Json.JsonSerializer.Serialize(bad.Value));
    }

    [Fact]
    public async Task Consultar_SucessoDevolve200ComResultado()
    {
        var ctrl = new SqlController(new ServicoFake((_, _) => Resultado()));

        var r = await ctrl.Consultar(new SqlConsultaRequest("homolog", "select 1"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(r);
        Assert.IsType<SqlResultado>(ok.Value);
    }

    private static SqlResultado Resultado() => new([], [], false, 0);

    private sealed class ServicoFake(Func<string, string, SqlResultado> fn) : ISqlService
    {
        public Task<SqlResultado> ConsultarAsync(string ambiente, string sql, CancellationToken ct) => Task.FromResult(fn(ambiente, sql));
    }
}
