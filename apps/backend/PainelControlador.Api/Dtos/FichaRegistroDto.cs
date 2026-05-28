namespace PainelControlador.Api.Dtos;

public record FichaRegistroDto(
    // Empresa
    string Empresa,
    string? CnpjEmpresa,
    string? EnderecoEmpresa,
    string? BairroEmpresa,
    string? MunicipioEmpresa,
    string? UfEmpresa,
    string? CnaeEmpresa,
    // Identificação do empregado
    string Matricula,
    string Nome,
    string? Cpf,
    string? Pis,
    string? Nascimento,
    string? Sexo,
    string? EstadoCivil,
    string? Nacionalidade,
    string? NomeMae,
    string? NomePai,
    string? GrauInstrucao,
    // Endereço
    string? Endereco,
    string? Bairro,
    string? Cep,
    string? Municipio,
    string? Uf,
    // Documentos
    string? Rg,
    string? RgOrgao,
    string? RgExpedicao,
    string? RgUf,
    string? Ctps,
    string? CtpsSerie,
    string? CtpsExpedicao,
    string? CtpsUf,
    // Vínculo
    string? Cargo,
    string? CboEsocial,
    string? Departamento,
    string? Setor,
    string? Admissao,
    string? Desligamento,
    decimal? SalarioAtual,
    string? HorasMensais,
    string? Sindicato,
    string? FormaRemuneracao,
    string? EmailContato,
    string? Celular,
    // Históricos
    List<AlteracaoCargoDto> AlteracoesCargo,
    List<AlteracaoSalarialDto> AlteracoesSalariais,
    List<FeriasDto> Ferias,
    List<AfastamentoDto> Afastamentos
);

public record AlteracaoCargoDto(string Data, string Cargo, string? Motivo, string? CboEsocial);
public record AlteracaoSalarialDto(string Data, decimal Salario, decimal? Indice, string? Motivo);
public record FeriasDto(string PeriodoAquisitivo, string? PeriodoGozo, int? DiasGozo, int? DiasAbono, int? DiasLicenca);
public record AfastamentoDto(string Periodo, string? Afastamento, int? QtdDias);
