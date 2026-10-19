namespace PainelControlador.Api.Configuration;

public class TrocaNumeroOptions
{
    public const string SectionName = "TrocaNumero";
    public string? WebhookUrl { get; set; }
    public string? Token { get; set; }
}
