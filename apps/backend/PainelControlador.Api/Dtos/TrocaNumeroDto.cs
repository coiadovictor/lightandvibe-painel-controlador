namespace PainelControlador.Api.Dtos;

public record TrocaNumeroDto(long Id, string Matricula, string? Nome, string CelularAntigo, string CelularNovo, DateTime CriadoEm);
public record DecisaoTrocaResultado(bool Ok, bool SessaoAntigaInvalidada);
