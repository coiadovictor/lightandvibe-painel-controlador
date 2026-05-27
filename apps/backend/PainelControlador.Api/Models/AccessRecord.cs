namespace PainelControlador.Api.Models;

public class AccessRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
}
