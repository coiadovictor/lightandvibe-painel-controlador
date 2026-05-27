using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}
