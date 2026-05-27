namespace PainelControlador.Api.Models;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? EmployeeId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
