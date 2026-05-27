namespace PainelControlador.Api.Dtos;

public record EmployeeDto(
    string Id,
    string Name,
    string? Email,
    string? Department,
    string? Phone,
    DateTime? LastContactAt
);
