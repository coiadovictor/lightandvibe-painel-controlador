namespace PainelControlador.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "painel-controlador";
    public string Audience { get; set; } = "painel-frontend";
    public int ExpiresInMinutes { get; set; } = 480;
}
