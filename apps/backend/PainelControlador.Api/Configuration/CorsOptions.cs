namespace PainelControlador.Api.Configuration;

public class CorsOptions
{
    public const string SectionName = "Cors";
    public string AllowedOrigins { get; set; } = string.Empty;

    public string[] GetOrigins() =>
        string.IsNullOrWhiteSpace(AllowedOrigins)
            ? Array.Empty<string>()
            : AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
