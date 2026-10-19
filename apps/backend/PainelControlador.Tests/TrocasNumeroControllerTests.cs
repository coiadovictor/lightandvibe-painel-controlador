using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Dtos;
using PainelControlador.Api.Services;
using Xunit;

namespace PainelControlador.Tests;

public class TrocasNumeroControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public TrocasNumeroControllerTests(CustomWebApplicationFactory factory) => _factory = factory;

    private sealed class ServicoFalso : ITrocaNumeroService
    {
        public Task<List<TrocaNumeroDto>> ListarPendentesAsync(CancellationToken ct = default) => Task.FromResult(new List<TrocaNumeroDto>());
        public Task<int> ContarPendentesAsync(CancellationToken ct = default) => Task.FromResult(3);
        public Task<DecisaoTrocaResultado> AutorizarAsync(long id, string usuario, CancellationToken ct = default) => Task.FromResult(new DecisaoTrocaResultado(id != 99, true));
        public Task<DecisaoTrocaResultado> RecusarAsync(long id, string usuario, CancellationToken ct = default) => Task.FromResult(new DecisaoTrocaResultado(true, false));
    }

    private WebApplicationFactory<Program> FabricaComServicoFalso()
        => _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ITrocaNumeroService, ServicoFalso>()));

    private static string TokenDeTeste(WebApplicationFactory<Program> fabrica)
    {
        var jwt = fabrica.Services.GetRequiredService<JwtOptions>();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: [new Claim(ClaimTypes.Name, "rh.teste"), new Claim(JwtRegisteredClaimNames.Sub, "rh.teste")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task SemToken_Retorna401()
    {
        var resp = await FabricaComServicoFalso().CreateClient().GetAsync("/api/trocas-numero");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Count_ComToken_RetornaTotal()
    {
        var fabrica = FabricaComServicoFalso();
        var client = fabrica.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TokenDeTeste(fabrica));
        var resp = await client.GetAsync("/api/trocas-numero/pendentes/count");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("\"total\":3", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Autorizar_Invalida_Retorna409()
    {
        var fabrica = FabricaComServicoFalso();
        var client = fabrica.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TokenDeTeste(fabrica));
        var resp = await client.PostAsync("/api/trocas-numero/99/autorizar", null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Autorizar_Valida_Retorna200ComFlag()
    {
        var fabrica = FabricaComServicoFalso();
        var client = fabrica.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TokenDeTeste(fabrica));
        var resp = await client.PostAsync("/api/trocas-numero/5/autorizar", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("\"sessaoAntigaInvalidada\":true", await resp.Content.ReadAsStringAsync());
    }
}
