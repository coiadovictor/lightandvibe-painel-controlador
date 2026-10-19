using PainelControlador.Api.Services;
using Xunit;

namespace PainelControlador.Tests;

public class N8nDbServiceTelefoneTests
{
    [Fact]
    public void FormatPhone_RemoveDdiEmNumeroDe13Digitos()
    {
        Assert.Equal("(12) 98199-3160", N8nDbService.FormatPhone("5512981993160"));
    }

    [Fact]
    public void FormatPhone_RemoveDdiEmNumeroDe12Digitos()
    {
        Assert.Equal("(49) 9944-1516", N8nDbService.FormatPhone("554999441516"));
    }

    [Fact]
    public void ResolveContact_EncontraFuncionarioQuandoJidNaoTemNonoDigito()
    {
        var mapa = new Dictionary<string, (string Name, string? Setor)>
        {
            ["49999441516"] = ("Fulana", "TECNOLOGIA DA INFORMAÇÃO"),
        };
        var (telefone, nome, setor) = N8nDbService.ResolveContact("554999441516@s.whatsapp.net", mapa);
        Assert.Equal("(49) 9944-1516", telefone);
        Assert.Equal("Fulana", nome);
        Assert.Equal("TECNOLOGIA DA INFORMAÇÃO", setor);
    }

    [Fact]
    public void ResolveContact_UsaSoOCelularQuandoSessaoTemInicioDepoisDoDoisPontos()
    {
        var mapa = new Dictionary<string, (string Name, string? Setor)>
        {
            ["11992929439"] = ("Luis", "RECURSOS HUMANOS"),
        };
        var (telefone, nome, setor) = N8nDbService.ResolveContact("5511992929439:1792429617", mapa);
        Assert.Equal("(11) 99292-9439", telefone);
        Assert.Equal("Luis", nome);
        Assert.Equal("RECURSOS HUMANOS", setor);
    }

    [Fact]
    public void ResolveContact_FormataCelularSemCadastroQuandoSessaoTemInicio()
    {
        var (telefone, nome, _) = N8nDbService.ResolveContact("5512981993160:1792429617", new());
        Assert.Equal("(12) 98199-3160", telefone);
        Assert.Null(nome);
    }
}
