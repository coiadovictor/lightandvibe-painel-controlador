namespace PainelControlador.Api.Configuration;

/// <summary>
/// Configuração da leitura de logs/saúde dos containers via Docker Engine API.
/// Lida a partir de variáveis de ambiente (MONITORED_CONTAINERS, DOCKER_SOCKET_PATH).
/// </summary>
public class DockerOptions
{
    public const string DefaultSocketPath = "/var/run/docker.sock";

    /// <summary>Caminho do unix socket do Docker (montado read-only no container).</summary>
    public string SocketPath { get; set; } = DefaultSocketPath;

    /// <summary>Containers monitorados, na ordem de exibição.</summary>
    public IReadOnlyList<MonitoredContainer> Containers { get; set; } = Array.Empty<MonitoredContainer>();

    /// <summary>
    /// Faz o parse da env var MONITORED_CONTAINERS (CSV). Cada item é
    /// "matcher" ou "alias=matcher". Ex.: "Evolution=n8n_evolution-api,n8n=n8n_n8n".
    /// </summary>
    public static DockerOptions FromConfiguration(IConfiguration config)
    {
        var socketPath = config["DOCKER_SOCKET_PATH"];
        var raw = config["MONITORED_CONTAINERS"] ?? "";

        var containers = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseEntry)
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        return new DockerOptions
        {
            SocketPath = string.IsNullOrWhiteSpace(socketPath) ? DefaultSocketPath : socketPath,
            Containers = containers,
        };
    }

    private static MonitoredContainer? ParseEntry(string entry)
    {
        var eq = entry.IndexOf('=');
        if (eq > 0)
        {
            var alias = entry[..eq].Trim();
            var matcher = entry[(eq + 1)..].Trim();
            if (matcher.Length == 0) return null;
            return new MonitoredContainer(alias, matcher);
        }

        return entry.Length == 0 ? null : new MonitoredContainer(entry, entry);
    }
}

/// <param name="Alias">Nome amigável exibido na UI.</param>
/// <param name="Matcher">Prefixo de serviço usado pra casar o nome do container.</param>
public record MonitoredContainer(string Alias, string Matcher);
