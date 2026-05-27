namespace PainelControlador.Api.Dtos;

public record HollerithLinhaDto(
    int Codigo,
    string Descricao,
    string Quantidade,
    decimal? Vencimento,
    decimal? Desconto
);

public record HollerithDto(
    string Matricula,
    string Nome,
    string Cpf,
    string Empresa,
    string CnpjEmpresa,
    string Local,
    string Secao,
    string Cargo,
    string Admissao,
    string Pis,
    int Mes,
    int Ano,
    List<HollerithLinhaDto> Linhas,
    decimal TotalVencimentos,
    decimal TotalDescontos,
    decimal Liquido
);
