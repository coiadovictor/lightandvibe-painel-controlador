namespace PainelControlador.Api.Configuration;

/// <summary>
/// Configuração para consultar o estado das instâncias do WhatsApp na Evolution API.
/// Lida de variáveis de ambiente (EVOLUTION_API_URL, EVOLUTION_API_KEY).
/// A chave NUNCA deve ir para o repositório — defina como env var no EasyPanel.
/// </summary>
public class EvolutionOptions
{
    /// <summary>Base URL da Evolution API. Em produção, prefira o DNS interno do Swarm.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>AUTHENTICATION_API_KEY da Evolution.</summary>
    public string ApiKey { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);

    public static EvolutionOptions FromConfiguration(IConfiguration config) => new()
    {
        BaseUrl = (config["EVOLUTION_API_URL"] ?? "").Trim().TrimEnd('/'),
        ApiKey = (config["EVOLUTION_API_KEY"] ?? "").Trim(),
    };
}
