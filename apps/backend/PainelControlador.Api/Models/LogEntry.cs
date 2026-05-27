namespace PainelControlador.Api.Models;

public class LogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Level { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
