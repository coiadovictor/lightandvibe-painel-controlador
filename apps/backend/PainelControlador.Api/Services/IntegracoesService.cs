using System.Text.Json.Serialization;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface IIntegracoesService
{
    Task<IReadOnlyList<IntegracaoDto>> ListAsync(string? situacao, string? tabela, int limit, CancellationToken ct);
}

public class IntegracoesService : IIntegracoesService
{
    private readonly SupabaseRestClient _sb;

    public IntegracoesService(SupabaseRestClient sb) => _sb = sb;

    public async Task<IReadOnlyList<IntegracaoDto>> ListAsync(
        string? situacao, string? tabela, int limit, CancellationToken ct)
    {
        var filters = new List<string>
        {
            $"order=data_inicio.desc",
            $"limit={Math.Clamp(limit, 1, 500)}"
        };

        if (!string.IsNullOrWhiteSpace(situacao))
            filters.Add($"situacao=eq.{situacao}");

        if (!string.IsNullOrWhiteSpace(tabela))
            filters.Add($"tabela=eq.{tabela}");

        var rows = await _sb.GetAsync<IntegracaoRow>(
            "controle_integracao", string.Join("&", filters), ct);

        return rows.Select(r => new IntegracaoDto(
            r.Id,
            r.Endpoint,
            r.Tabela,
            r.Acao,
            r.Origem,
            r.QuantidadeRegistros,
            r.Situacao,
            r.MensagemErro,
            r.DataInicio,
            r.DataFim
        )).ToList();
    }

    private record IntegracaoRow(
        [property: JsonPropertyName("id")]                   long Id,
        [property: JsonPropertyName("endpoint")]             string Endpoint,
        [property: JsonPropertyName("tabela")]               string Tabela,
        [property: JsonPropertyName("acao")]                 string? Acao,
        [property: JsonPropertyName("origem")]               string? Origem,
        [property: JsonPropertyName("quantidade_registros")] int? QuantidadeRegistros,
        [property: JsonPropertyName("situacao")]             string Situacao,
        [property: JsonPropertyName("mensagem_erro")]        string? MensagemErro,
        [property: JsonPropertyName("data_inicio")]          DateTime DataInicio,
        [property: JsonPropertyName("data_fim")]             DateTime? DataFim
    );
}
