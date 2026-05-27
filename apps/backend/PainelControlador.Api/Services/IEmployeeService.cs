using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken ct = default);
    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
