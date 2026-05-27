namespace PainelControlador.Api.Configuration;

public class N8nOptions
{
    public const string SectionName = "N8n";
    public string ConnectionString { get; set; } = "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}
