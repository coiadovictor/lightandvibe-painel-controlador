using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface ILogService
{
    Task<IReadOnlyList<LogEntryDto>> ListAsync(CancellationToken ct = default);
}
