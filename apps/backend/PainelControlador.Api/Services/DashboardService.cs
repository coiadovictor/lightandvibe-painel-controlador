using System.Text.Json.Serialization;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly SupabaseRestClient _sb;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(SupabaseRestClient sb, ILogger<DashboardService> logger)
    {
        _sb = sb;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        try
        {
            // Busca acessos e conversas em paralelo
            var acessosTask = _sb.GetAsync<AcessoRow>("pag_funcionario_acesso", ct: ct);
            var totalConversasTask = _sb.CountAsync("conversas", ct: ct);

            await Task.WhenAll(acessosTask, totalConversasTask);

            var acessos = await acessosTask;
            var totalConversations = await totalConversasTask;

            // Funcionários que acessaram = matrículas distintas em pag_funcionario_acesso
            var totalEmployeesLogged = acessos.Select(a => a.Matricula).Where(m => !string.IsNullOrEmpty(m)).Distinct().Count();
            var totalAccesses = acessos.Count;

            // Acessos por funcionário — agrupa por matrícula
            var grouped = acessos
                .GroupBy(a => a.Matricula)
                .Select(g => new { Matricula = g.Key ?? "", Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(20)
                .ToList();

            // Busca nomes para as matrículas
            var nameMap = new Dictionary<string, string>();
            var matriculas = grouped.Select(g => g.Matricula).Where(m => !string.IsNullOrEmpty(m)).ToList();
            if (matriculas.Count > 0)
            {
                var inFilter = string.Join(",", matriculas);
                var funcs = await _sb.GetAsync<FuncRow>("view_func", $"funcionario=in.({inFilter})", ct);
                nameMap = funcs.ToDictionary(f => f.Funcionario, f => f.Nome ?? f.Funcionario);
            }

            var accessesByEmployee = grouped
                .Select(g => new AccessByEmployeeDto(
                    EmployeeId: g.Matricula,
                    EmployeeName: nameMap.GetValueOrDefault(g.Matricula, g.Matricula),
                    AccessCount: g.Count))
                .ToList();

            return new DashboardSummaryDto(totalEmployeesLogged, totalAccesses, totalConversations, accessesByEmployee);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao montar dashboard summary.");
            return new DashboardSummaryDto(0, 0, 0, []);
        }
    }

    private record AcessoRow(
        [property: JsonPropertyName("matricula")] string? Matricula
    );

    private record FuncRow(
        [property: JsonPropertyName("funcionario")] string Funcionario,
        [property: JsonPropertyName("nome")] string? Nome
    );
}
