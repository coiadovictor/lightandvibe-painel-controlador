namespace PainelControlador.Api.Dtos;

public record AccessByEmployeeDto(
    string EmployeeId,
    string EmployeeName,
    int AccessCount
);

public record DashboardSummaryDto(
    int TotalEmployeesLogged,
    int TotalAccesses,
    int TotalConversations,
    IReadOnlyList<AccessByEmployeeDto> AccessesByEmployee
);
