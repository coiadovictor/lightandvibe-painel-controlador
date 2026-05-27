namespace PainelControlador.Api.Dtos;

public record LogStatsDto(
    long TotalMensagens,
    long SessoesUnicas,
    long MensagensHoje,
    long MensagensUltimaHora,
    long MensagensHumanas,
    long MensagensIA,
    double MediaMensagensPorSessao,
    List<SetorSessionsDto> SessoesPorSetor
);

public record SetorSessionsDto(
    string Setor,
    List<SessaoResumoDto> Sessoes
);

public record SessaoResumoDto(
    string SessionId,
    int TotalMensagens,
    DateTime? UltimaMensagem,
    string? Telefone = null,
    string? NomeFuncionario = null,
    string? Setor = null
);

public record LogEntryN8nDto(
    string Id,
    string SessionId,
    string Tipo,
    string Conteudo,
    DateTime? CriadoEm,
    string? NomeFuncionario = null
);
