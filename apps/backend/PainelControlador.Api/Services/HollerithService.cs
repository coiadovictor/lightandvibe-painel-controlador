using System.Text.Json.Serialization;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface IHollerithService
{
    Task<HollerithDto?> GetAsync(string matricula, int mes, int ano, CancellationToken ct);
}

public class HollerithService : IHollerithService
{
    private readonly SupabaseRestClient _sb;
    private readonly ILogger<HollerithService> _logger;

    public HollerithService(SupabaseRestClient sb, ILogger<HollerithService> logger)
    {
        _sb = sb;
        _logger = logger;
    }

    public async Task<HollerithDto?> GetAsync(string matricula, int mes, int ano, CancellationToken ct)
    {
        try
        {
            // 1. Employee data
            var funcRows = await _sb.GetAsync<ViewFuncRow>(
                "view_func", $"funcionario=eq.{Uri.EscapeDataString(matricula)}", ct);
            var func = funcRows.FirstOrDefault();
            if (func is null)
            {
                _logger.LogWarning("Funcionário {Matricula} não encontrado em view_func.", matricula);
                return null;
            }

            // 2. Movimentos for the period
            var movimentos = await _sb.GetAsync<MovimentoHistRow>(
                "pag_movimento_hist",
                $"funcionario=eq.{Uri.EscapeDataString(matricula)}&mes=eq.{mes}&ano=eq.{ano}&order=verba.asc",
                ct);

            if (movimentos.Count == 0)
            {
                _logger.LogWarning("Sem movimentos para {Matricula} em {Mes}/{Ano}.", matricula, mes, ano);
                return null;
            }

            // 3. Verba descriptions
            var verbaCodes = string.Join(",", movimentos.Select(m => m.Verba).Distinct());
            var verbas = await _sb.GetAsync<VerbaRow>(
                "pag_verba", $"verba=in.({verbaCodes})", ct);
            var verbaDict = verbas.ToDictionary(v => v.Verba);

            // 4. Company info via filial
            string empresa = "LIGHT AND VIBE AI AUTOMATION";
            string cnpj = "";
            if (!string.IsNullOrEmpty(func.FilialDestinatario))
            {
                var deptos = await _sb.GetAsync<ViewDeptoRow>(
                    "view_depto", $"destinatario=eq.{func.FilialDestinatario}", ct);
                var depto = deptos.FirstOrDefault();
                if (depto is not null)
                {
                    empresa = depto.Nome ?? empresa;
                    cnpj = FormatCnpj(depto.CgcCpf ?? "");
                }
            }

            // 5. Salário — verba 1014 quando presente, senão null
            decimal? salario = movimentos.FirstOrDefault(m => m.Verba == 1014)?.Valor;

            // 6. Build lines — only tipo 1 (vencimento) and tipo 2 (desconto)
            var linhas = new List<HollerithLinhaDto>();
            foreach (var mov in movimentos)
            {
                if (!verbaDict.TryGetValue(mov.Verba, out var verba)) continue;
                if (verba.Tipo != 1 && verba.Tipo != 2) continue;
                if (mov.Verba == 0) continue;

                var qtd = mov.Dias > 0 ? mov.Dias.ToString()
                        : mov.Horas > 0 ? $"{mov.Horas}h"
                        : "";

                linhas.Add(new HollerithLinhaDto(
                    mov.Verba,
                    verba.Descricao ?? $"Verba {mov.Verba}",
                    qtd,
                    verba.Tipo == 1 ? mov.Valor : null,
                    verba.Tipo == 2 ? mov.Valor : null
                ));
            }

            var totalVenc = linhas.Where(l => l.Vencimento.HasValue).Sum(l => l.Vencimento!.Value);
            var totalDesc = linhas.Where(l => l.Desconto.HasValue).Sum(l => l.Desconto!.Value);

            return new HollerithDto(
                matricula,
                func.Nome ?? "",
                FormatCpf(func.Cpf ?? ""),
                empresa,
                cnpj,
                func.Departamento ?? func.Diretoria ?? "",
                func.Setor ?? "",
                func.Cargo ?? "",
                func.Admissao ?? "",
                func.Pis ?? "",
                salario,
                mes,
                ano,
                linhas,
                totalVenc,
                totalDesc,
                totalVenc - totalDesc
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao montar holerite para {Matricula} {Mes}/{Ano}.", matricula, mes, ano);
            return null;
        }
    }

    private static string FormatCpf(string cpf)
    {
        cpf = new string(cpf.Where(char.IsDigit).ToArray());
        return cpf.Length == 11
            ? $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}"
            : cpf;
    }

    private static string FormatCnpj(string cnpj)
    {
        cnpj = new string(cnpj.Where(char.IsDigit).ToArray());
        return cnpj.Length == 14
            ? $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}"
            : cnpj;
    }

    // --- Internal row types ---

    private record ViewFuncRow(
        [property: JsonPropertyName("funcionario")] string? Funcionario,
        [property: JsonPropertyName("nome")] string? Nome,
        [property: JsonPropertyName("cpf")] string? Cpf,
        [property: JsonPropertyName("pis")] string? Pis,
        [property: JsonPropertyName("departamento")] string? Departamento,
        [property: JsonPropertyName("diretoria")] string? Diretoria,
        [property: JsonPropertyName("setor")] string? Setor,
        [property: JsonPropertyName("cargo")] string? Cargo,
        [property: JsonPropertyName("admissao")] string? Admissao,
        [property: JsonPropertyName("filialdestinatario")] string? FilialDestinatario
    );

    private record MovimentoHistRow(
        [property: JsonPropertyName("funcionario")] string Funcionario,
        [property: JsonPropertyName("mes")] int Mes,
        [property: JsonPropertyName("ano")] int Ano,
        [property: JsonPropertyName("verba")] int Verba,
        [property: JsonPropertyName("dias")] int Dias,
        [property: JsonPropertyName("horas")] int Horas,
        [property: JsonPropertyName("valor")] decimal Valor
    );

    private record VerbaRow(
        [property: JsonPropertyName("verba")] int Verba,
        [property: JsonPropertyName("descricao")] string? Descricao,
        [property: JsonPropertyName("tipo")] int? Tipo
    );

    private record ViewDeptoRow(
        [property: JsonPropertyName("destinatario")] string? Destinatario,
        [property: JsonPropertyName("nome")] string? Nome,
        [property: JsonPropertyName("cgccpf")] string? CgcCpf
    );
}
