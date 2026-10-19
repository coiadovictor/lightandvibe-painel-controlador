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
}
