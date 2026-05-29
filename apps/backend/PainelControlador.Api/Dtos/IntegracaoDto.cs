namespace PainelControlador.Api.Dtos;

public record IntegracaoDto(
    long Id,
    string Endpoint,
    string Tabela,
    string? Acao,
    string? Origem,
    int? QuantidadeRegistros,
    string Situacao,
    string? MensagemErro,
    DateTime DataInicio,
    DateTime? DataFim
);
