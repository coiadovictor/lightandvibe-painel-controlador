using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface IDockerLogsService
{
    Task<AmbienteOverviewDto> GetOverviewAsync(int windowHours, CancellationToken ct);
    Task<ContainerLogsDto> GetLogsAsync(string alias, int tail, CancellationToken ct);
}

/// <summary>
/// Cliente read-only da Docker Engine API via unix socket. Lê saúde e logs dos
/// containers monitorados. Nunca executa operações de escrita (start/stop/restart).
/// </summary>
public partial class DockerLogsService : IDockerLogsService
{
    private const int IncidentScanTail = 5000;  // teto de linhas varridas na janela
    private const int MaxLogIncidentsPerContainer = 200;
    private const int DefaultWindowHours = 48;
    private const int MaxWindowHours = 48;

    private readonly DockerOptions _opts;
    private readonly ILogger<DockerLogsService> _logger;
    private readonly HttpClient _http;

    // Padrões que indicam queda/desconexão/sobrecarga nos logs.
    [GeneratedRegex(@"disconnect|connection closed|logout|out of memory|fatal|econnrefused|econnreset|panic|rate limit|\b429\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex IncidentPattern();

    // Prefixo de timestamp RFC3339 que o Docker adiciona quando timestamps=1.
    [GeneratedRegex(@"^(\S+?)\s(.*)$", RegexOptions.Compiled)]
    private static partial Regex TimestampedLine();

    public DockerLogsService(DockerOptions opts, ILogger<DockerLogsService> logger)
    {
        _opts = opts;
        _logger = logger;

        var socketPath = _opts.SocketPath;
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (_, token) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), token);
                return new NetworkStream(socket, ownsSocket: true);
            },
        };

        _http = new HttpClient(handler)
        {
            // Host é ignorado pelo daemon; usamos paths não-versionados da Engine API.
            BaseAddress = new Uri("http://localhost"),
            Timeout = TimeSpan.FromSeconds(15),
        };
    }

    private bool SocketAvailable() => File.Exists(_opts.SocketPath);

    public async Task<AmbienteOverviewDto> GetOverviewAsync(int windowHours, CancellationToken ct)
    {
        windowHours = Math.Clamp(windowHours <= 0 ? DefaultWindowHours : windowHours, 1, MaxWindowHours);
        var sinceUnix = DateTimeOffset.UtcNow.AddHours(-windowHours).ToUnixTimeSeconds();

        if (_opts.Containers.Count == 0)
            return new AmbienteOverviewDto(false,
                "Nenhum container configurado em MONITORED_CONTAINERS.",
                windowHours, Array.Empty<ContainerHealthDto>(), Array.Empty<IncidentDto>());

        if (!SocketAvailable())
            return new AmbienteOverviewDto(false,
                $"Docker socket não disponível em {_opts.SocketPath}. Monte /var/run/docker.sock:ro no container do backend.",
                windowHours, Array.Empty<ContainerHealthDto>(), Array.Empty<IncidentDto>());

        List<DockerContainer> running;
        try
        {
            running = await ListContainersAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao listar containers via Docker API");
            return new AmbienteOverviewDto(false,
                $"Não foi possível falar com o Docker: {ex.Message}",
                windowHours, Array.Empty<ContainerHealthDto>(), Array.Empty<IncidentDto>());
        }

        var healthList = new List<ContainerHealthDto>();
        var incidents = new List<IncidentDto>();

        foreach (var mon in _opts.Containers)
        {
            var match = running.FirstOrDefault(c => Matches(c.Name, mon.Matcher));
            if (match is null)
            {
                healthList.Add(new ContainerHealthDto(
                    mon.Alias, mon.Matcher, null, null, false, "not_found",
                    0, false, 0, null, null));
                continue;
            }

            ContainerInspect? inspect = null;
            try { inspect = await InspectAsync(match.Id, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Falha ao inspecionar {Name}", match.Name); }

            var health = new ContainerHealthDto(
                mon.Alias, mon.Matcher, match.Name, ShortId(match.Id), true,
                inspect?.Status ?? "unknown",
                inspect?.RestartCount ?? 0,
                inspect?.OomKilled ?? false,
                inspect?.ExitCode ?? 0,
                inspect?.StartedAt,
                inspect?.Image ?? match.Image);
            healthList.Add(health);

            incidents.AddRange(StructuralIncidents(mon.Alias, health, inspect?.FinishedAt));

            // Varre a janela (since) em busca de padrões de queda.
            try
            {
                var lines = await FetchLogsAsync(match.Id, inspect?.Tty ?? false, IncidentScanTail, ct, sinceUnix);
                incidents.AddRange(LogIncidents(mon.Alias, lines));
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Falha ao ler logs de {Name}", match.Name); }
        }

        var ordered = incidents
            .OrderByDescending(i => i.Timestamp ?? DateTime.MinValue)
            .ToList();

        return new AmbienteOverviewDto(true, null, windowHours, healthList, ordered);
    }

    public async Task<ContainerLogsDto> GetLogsAsync(string alias, int tail, CancellationToken ct)
    {
        tail = Math.Clamp(tail, 50, 2000);

        var mon = _opts.Containers.FirstOrDefault(c =>
            string.Equals(c.Alias, alias, StringComparison.OrdinalIgnoreCase));
        if (mon is null)
            return new ContainerLogsDto(false, $"Container '{alias}' não está na lista monitorada.",
                alias, Array.Empty<LogLineDto>());

        if (!SocketAvailable())
            return new ContainerLogsDto(false,
                $"Docker socket não disponível em {_opts.SocketPath}.",
                alias, Array.Empty<LogLineDto>());

        try
        {
            var running = await ListContainersAsync(ct);
            var match = running.FirstOrDefault(c => Matches(c.Name, mon.Matcher));
            if (match is null)
                return new ContainerLogsDto(true, "Container não encontrado em execução.",
                    alias, Array.Empty<LogLineDto>());

            var inspect = await InspectAsync(match.Id, ct);
            var lines = await FetchLogsAsync(match.Id, inspect?.Tty ?? false, tail, ct);
            return new ContainerLogsDto(true, null, alias, lines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao buscar logs de {Alias}", alias);
            return new ContainerLogsDto(false, $"Erro ao buscar logs: {ex.Message}",
                alias, Array.Empty<LogLineDto>());
        }
    }

    // ---------- incidentes ----------

    private static IEnumerable<IncidentDto> StructuralIncidents(
        string alias, ContainerHealthDto h, DateTime? finishedAt)
    {
        if (h.OomKilled)
            yield return new IncidentDto(finishedAt ?? h.StartedAt, alias, "oom", "critical",
                "O serviço foi derrubado por falta de memória no servidor.", null);

        if (h.RestartCount > 0)
            yield return new IncidentDto(h.StartedAt, alias, "restart", "warning",
                $"O serviço reiniciou sozinho {h.RestartCount} vez(es). Voltou a funcionar em {Fmt(h.StartedAt)}.", null);

        if (string.Equals(h.Status, "exited", StringComparison.OrdinalIgnoreCase) && h.ExitCode != 0)
            yield return new IncidentDto(finishedAt, alias, "exit", "error",
                "O serviço parou de funcionar inesperadamente.", null);
    }

    private static IEnumerable<IncidentDto> LogIncidents(string alias, IReadOnlyList<LogLineDto> lines)
    {
        var hits = new List<IncidentDto>();
        foreach (var line in lines)
        {
            if (!IncidentPattern().IsMatch(line.Text)) continue;
            var severity = line.Stream == "stderr" ? "error" : "warning";
            // Mensagem em linguagem clara + a linha crua guardada como detalhe técnico.
            hits.Add(new IncidentDto(line.Timestamp, alias, "log", severity,
                FriendlyLogReason(line.Text), line.Text.Trim()));
        }
        // mantém só os mais recentes pra não inundar a timeline
        return hits
            .OrderByDescending(i => i.Timestamp ?? DateTime.MinValue)
            .Take(MaxLogIncidentsPerContainer);
    }

    /// <summary>Traduz um padrão técnico de log para algo que o atendimento entende.</summary>
    private static string FriendlyLogReason(string text)
    {
        var t = text.ToLowerInvariant();
        if (t.Contains("out of memory")) return "Faltou memória no servidor.";
        if (t.Contains("disconnect") || t.Contains("connection closed") || t.Contains("logout"))
            return "A conexão caiu / o serviço desconectou (pode afetar o envio e recebimento de mensagens).";
        if (t.Contains("rate limit") || Regex.IsMatch(t, @"\b429\b"))
            return "Bloqueio temporário por excesso de requisições (rate limit).";
        if (t.Contains("econnrefused") || t.Contains("econnreset"))
            return "Falha de comunicação entre os serviços.";
        if (t.Contains("panic") || t.Contains("fatal"))
            return "Erro grave registrado no serviço.";
        return "Erro registrado no serviço.";
    }

    // ---------- Docker API ----------

    private async Task<List<DockerContainer>> ListContainersAsync(CancellationToken ct)
    {
        using var resp = await _http.GetAsync("/containers/json?all=1", ct);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var list = new List<DockerContainer>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var id = el.GetProperty("Id").GetString() ?? "";
            var name = el.TryGetProperty("Names", out var names) && names.GetArrayLength() > 0
                ? names[0].GetString()?.TrimStart('/') ?? ""
                : "";
            var image = el.TryGetProperty("Image", out var img) ? img.GetString() : null;
            list.Add(new DockerContainer(id, name, image));
        }
        return list;
    }

    private async Task<ContainerInspect?> InspectAsync(string id, CancellationToken ct)
    {
        using var resp = await _http.GetAsync($"/containers/{id}/json", ct);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var state = root.GetProperty("State");
        var config = root.TryGetProperty("Config", out var cfg) ? cfg : default;

        return new ContainerInspect(
            Status: state.TryGetProperty("Status", out var st) ? st.GetString() ?? "unknown" : "unknown",
            RestartCount: root.TryGetProperty("RestartCount", out var rc) ? rc.GetInt32() : 0,
            OomKilled: state.TryGetProperty("OOMKilled", out var oom) && oom.GetBoolean(),
            ExitCode: state.TryGetProperty("ExitCode", out var ec) ? ec.GetInt32() : 0,
            StartedAt: ParseDate(state, "StartedAt"),
            FinishedAt: ParseDate(state, "FinishedAt"),
            Tty: config.ValueKind == JsonValueKind.Object
                 && config.TryGetProperty("Tty", out var tty) && tty.GetBoolean(),
            Image: config.ValueKind == JsonValueKind.Object
                   && config.TryGetProperty("Image", out var im) ? im.GetString() : null);
    }

    private async Task<IReadOnlyList<LogLineDto>> FetchLogsAsync(
        string id, bool tty, int tail, CancellationToken ct, long? sinceUnix = null)
    {
        var url = $"/containers/{id}/logs?stdout=1&stderr=1&timestamps=1&tail={tail}";
        if (sinceUnix.HasValue) url += $"&since={sinceUnix.Value}";
        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        return tty ? ParseRaw(bytes) : ParseMultiplexed(bytes);
    }

    // ---------- parsing de log ----------

    /// <summary>Container com TTY: stream cru, sem cabeçalho de multiplexação.</summary>
    private static IReadOnlyList<LogLineDto> ParseRaw(byte[] bytes)
    {
        var text = Encoding.UTF8.GetString(bytes);
        return SplitLines(text, "stdout");
    }

    /// <summary>
    /// Container sem TTY: stream multiplexado. Cada frame tem header de 8 bytes
    /// [tipo,0,0,0, len(4 big-endian)] seguido do payload. Junta payloads por
    /// stream, decodifica e quebra em linhas.
    /// </summary>
    private static IReadOnlyList<LogLineDto> ParseMultiplexed(byte[] bytes)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        int i = 0;
        while (i + 8 <= bytes.Length)
        {
            var streamType = bytes[i];
            var len = (int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(i + 4, 4));
            i += 8;
            if (len <= 0 || i + len > bytes.Length)
            {
                if (i >= bytes.Length) break;
                len = Math.Min(len, bytes.Length - i);
                if (len <= 0) break;
            }

            var payload = Encoding.UTF8.GetString(bytes, i, len);
            (streamType == 2 ? stderr : stdout).Append(payload);
            i += len;
        }

        var lines = new List<LogLineDto>();
        lines.AddRange(SplitLines(stdout.ToString(), "stdout"));
        lines.AddRange(SplitLines(stderr.ToString(), "stderr"));
        return lines
            .OrderBy(l => l.Timestamp ?? DateTime.MinValue)
            .ToList();
    }

    private static IReadOnlyList<LogLineDto> SplitLines(string text, string stream)
    {
        var result = new List<LogLineDto>();
        if (string.IsNullOrEmpty(text)) return result;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0) continue;

            var m = TimestampedLine().Match(line);
            if (m.Success && DateTime.TryParse(m.Groups[1].Value, null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var ts))
            {
                result.Add(new LogLineDto(ts, stream, m.Groups[2].Value));
            }
            else
            {
                result.Add(new LogLineDto(null, stream, line));
            }
        }
        return result;
    }

    // ---------- helpers ----------

    private static bool Matches(string name, string matcher)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (string.Equals(name, matcher, StringComparison.OrdinalIgnoreCase)) return true;
        // Swarm: "stack_service.replica.taskid" — casa pelo prefixo de serviço + "."
        if (name.StartsWith(matcher + ".", StringComparison.OrdinalIgnoreCase)) return true;
        return name.Contains(matcher, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime? ParseDate(JsonElement state, string prop)
    {
        if (!state.TryGetProperty(prop, out var el)) return null;
        var s = el.GetString();
        if (string.IsNullOrEmpty(s)) return null;
        // Docker usa "0001-01-01T00:00:00Z" para "nunca".
        if (s.StartsWith("0001-01-01")) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var dt) ? dt : null;
    }

    private static string ShortId(string id) => id.Length > 12 ? id[..12] : id;

    private static string Fmt(DateTime? dt) => dt?.ToString("dd/MM HH:mm") ?? "—";

    // ---------- modelos internos ----------

    private record DockerContainer(string Id, string Name, string? Image);

    private record ContainerInspect(
        string Status, int RestartCount, bool OomKilled, int ExitCode,
        DateTime? StartedAt, DateTime? FinishedAt, bool Tty, string? Image);
}
