using System.Text.Json.Serialization;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public class EmployeeService : IEmployeeService
{
    private readonly SupabaseRestClient _sb;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(SupabaseRestClient sb, ILogger<EmployeeService> logger)
    {
        _sb = sb;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            // Funcionários = quem já acessou (pag_funcionario_acesso), com dados vindos de pag_funcionario.
            var acessos = await _sb.GetAsync<AcessoRow>("pag_funcionario_acesso", "order=ultimocontato.desc", ct);

            // Obtém matrículas únicas para fazer join com view_func
            var matriculas = acessos.Select(a => a.Matricula).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();
            if (matriculas.Count == 0) return [];

            var inFilter = string.Join(",", matriculas);
            var funcs = await _sb.GetAsync<FuncRow>("view_func", $"funcionario=in.({inFilter})", ct);
            var funcMap = funcs.ToDictionary(f => f.Funcionario, f => f);

            // Agrupa por matrícula mantendo o último contato por funcionário
            var grouped = acessos
                .GroupBy(a => a.Matricula)
                .Select(g =>
                {
                    var last = g.OrderByDescending(a => a.UltimoContato).First();
                    funcMap.TryGetValue(last.Matricula ?? "", out var func);

                    return new EmployeeDto(
                        Id: last.Matricula ?? last.Id.ToString(),
                        Name: func?.Nome ?? last.Matricula ?? "—",
                        Email: func?.EmailContato,
                        Department: func?.Departamento,
                        Phone: last.Celular,
                        LastContactAt: last.UltimoContato
                    );
                })
                .OrderByDescending(e => e.LastContactAt)
                .ToList();

            return grouped;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao listar funcionários via Supabase REST.");
            return [];
        }
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => (await ListAsync(ct)).FirstOrDefault(e => e.Id == id.ToString());

    // Raw rows from PostgREST — named to match snake_case JSON
    private record AcessoRow(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("celular")] string? Celular,
        [property: JsonPropertyName("cpf")] string? Cpf,
        [property: JsonPropertyName("matricula")] string? Matricula,
        [property: JsonPropertyName("primeirocontato")] DateTime? PrimeiroContato,
        [property: JsonPropertyName("ultimocontato")] DateTime? UltimoContato
    );

    private record FuncRow(
        [property: JsonPropertyName("funcionario")] string Funcionario,
        [property: JsonPropertyName("nome")] string? Nome,
        [property: JsonPropertyName("emailcontato")] string? EmailContato,
        [property: JsonPropertyName("departamento")] string? Departamento
    );
}
