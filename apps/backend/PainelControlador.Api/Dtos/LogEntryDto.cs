namespace PainelControlador.Api.Dtos;

public record LogEntryDto(
    string Id,
    string Level,
    string Message,
    string? Source,
    DateTime CreatedAt
);
