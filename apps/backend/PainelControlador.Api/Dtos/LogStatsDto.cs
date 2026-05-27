namespace PainelControlador.Api.Dtos;

public record LogStatsDto(
    long TotalMensagens,
    long SessoesUnicas,
    long MensagensHoje,
    long MensagensUltimaHora,
    long MensagensHumanas,
    long MensagensIA,
    double MediaMensagensPorSessao,
    List<SessaoResumoDto> UltimasSessoes
);

public record SessaoResumoDto(
    string SessionId,
    int TotalMensagens,
    DateTime? UltimaMensagem
);

public record LogEntryN8nDto(
    string Id,
    string SessionId,
    string Tipo,
    string Conteudo,
    DateTime CriadoEm
);
