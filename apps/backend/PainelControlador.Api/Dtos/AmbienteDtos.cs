namespace PainelControlador.Api.Dtos;

/// <summary>Saúde de um container monitorado (derivada de docker inspect).</summary>
public record ContainerHealthDto(
    string Alias,          // nome amigável configurado
    string Matcher,        // prefixo de serviço usado no match
    string? Name,          // nome real resolvido do container
    string? Id,            // id curto
    bool Found,            // o container foi encontrado?
    string Status,         // running / exited / restarting / not_found / unavailable ...
    int RestartCount,
    bool OomKilled,
    int ExitCode,
    DateTime? StartedAt,
    string? Image
);

/// <summary>Uma linha de log já demultiplexada.</summary>
public record LogLineDto(
    DateTime? Timestamp,
    string Stream,         // "stdout" | "stderr"
    string Text
);

/// <summary>Um incidente derivado (restart, OOM, exit, ou padrão no log).</summary>
public record IncidentDto(
    DateTime? Timestamp,
    string Container,      // alias
    string Type,           // restart | oom | exit | log
    string Severity,       // warning | error | critical
    string Message,        // texto claro, em português, para o time de atendimento
    string? Detail         // linha de log crua (modo técnico); null para incidentes estruturais
);

/// <summary>Visão geral: saúde de todos os monitorados + linha do tempo de incidentes.</summary>
public record AmbienteOverviewDto(
    bool Available,            // o socket está acessível?
    string? Message,           // mensagem quando indisponível
    int WindowHours,           // janela aplicada (horas) da linha do tempo
    IReadOnlyList<ContainerHealthDto> Containers,
    IReadOnlyList<IncidentDto> Incidents
);

/// <summary>Tail de log de um container específico.</summary>
public record ContainerLogsDto(
    bool Available,
    string? Message,
    string Container,          // alias
    IReadOnlyList<LogLineDto> Lines
);
