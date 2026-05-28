using System.Text.Json.Serialization;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface IFichaRegistroService
{
    Task<FichaRegistroDto?> GetAsync(string matricula, CancellationToken ct);
}

public class FichaRegistroService : IFichaRegistroService
{
    private readonly SupabaseRestClient _sb;
    private readonly ILogger<FichaRegistroService> _logger;

    public FichaRegistroService(SupabaseRestClient sb, ILogger<FichaRegistroService> logger)
    {
        _sb = sb;
        _logger = logger;
    }

    public async Task<FichaRegistroDto?> GetAsync(string matricula, CancellationToken ct)
    {
        try
        {
            // 1. Dados do funcionário (view_func completa)
            var funcRows = await _sb.GetAsync<ViewFuncFullRow>(
                "view_func", $"funcionario=eq.{Uri.EscapeDataString(matricula)}&select=*", ct);
            var func = funcRows.FirstOrDefault();
            if (func is null) return null;

            // 2. Dados da empresa via view_depto
            string empresa = "LIGHT AND VIBE AI AUTOMATION";
            string? cnpjEmpresa = null, endEmpresa = null, bairroEmpresa = null,
                    munEmpresa = null, ufEmpresa = null, cnaeEmpresa = null;

            if (!string.IsNullOrEmpty(func.FilialDestinatario))
            {
                var deptos = await _sb.GetAsync<ViewDeptoFullRow>(
                    "view_depto", $"destinatario=eq.{Uri.EscapeDataString(func.FilialDestinatario)}&select=*", ct);
                var depto = deptos.FirstOrDefault();
                if (depto is not null)
                {
                    empresa = depto.Nome ?? empresa;
                    cnpjEmpresa = FormatCnpj(depto.CgcCpf ?? "");
                    endEmpresa = depto.Endereco;
                    bairroEmpresa = depto.Bairro;
                    munEmpresa = depto.Municipio;
                    ufEmpresa = depto.Uf;
                    cnaeEmpresa = depto.Cnae;
                }
            }

            // 3. Dados pessoais complementares — pag_funcionario e fat_destinatario_pfisica em paralelo
            PagFuncionarioRow? pagFunc = null;
            FatPfisicaRow? pfisica = null;
            try
            {
                var pagFuncTask = _sb.GetAsync<PagFuncionarioRow>(
                    "pag_funcionario", $"funcionario=eq.{Uri.EscapeDataString(matricula)}&select=*", ct);
                var pfisicaTask = _sb.GetAsync<FatPfisicaRow>(
                    "fat_destinatario_pfisica", $"funcionario=eq.{Uri.EscapeDataString(matricula)}&select=*", ct);

                await Task.WhenAll(pagFuncTask, pfisicaTask);

                pagFunc = (await pagFuncTask).FirstOrDefault();
                pfisica = (await pfisicaTask).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Dados complementares não disponíveis para {Matricula}.", matricula);
                // tenta pag_funcionario individualmente se a outra falhou
                if (pagFunc is null)
                {
                    try
                    {
                        var rows = await _sb.GetAsync<PagFuncionarioRow>(
                            "pag_funcionario", $"funcionario=eq.{Uri.EscapeDataString(matricula)}&select=*", ct);
                        pagFunc = rows.FirstOrDefault();
                    }
                    catch { /* silencioso */ }
                }
            }

            // 4. Celular via pag_funcionario_acesso
            string? celular = null;
            try
            {
                var acessos = await _sb.GetAsync<AcessoRow>(
                    "pag_funcionario_acesso", $"matricula=eq.{Uri.EscapeDataString(matricula)}&order=ultimocontato.desc&limit=1", ct);
                celular = acessos.FirstOrDefault()?.Celular;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Celular não disponível para {Matricula}.", matricula);
            }

            // 5. Histórico salarial derivado de pag_movimento_hist (verba 1014)
            var alteracoesSalariais = new List<AlteracaoSalarialDto>();
            try
            {
                var movs = await _sb.GetAsync<MovimentoSalarioRow>(
                    "pag_movimento_hist",
                    $"funcionario=eq.{Uri.EscapeDataString(matricula)}&verba=eq.1014&order=ano.asc,mes.asc",
                    ct);

                decimal? ultimoSalario = null;
                foreach (var mov in movs)
                {
                    if (mov.Valor == ultimoSalario) continue;
                    var data = $"01/{mov.Mes:D2}/{mov.Ano}";
                    var motivo = ultimoSalario is null ? "Inicial" : "Alteração";
                    alteracoesSalariais.Add(new AlteracaoSalarialDto(data, mov.Valor, null, motivo));
                    ultimoSalario = mov.Valor;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Histórico salarial não disponível para {Matricula}.", matricula);
            }

            // 6. Histórico de cargo
            var alteracoesCargo = new List<AlteracaoCargoDto>();
            try
            {
                var cargos = await _sb.GetAsync<CargoHistRow>(
                    "pag_cargo_hist",
                    $"funcionario=eq.{Uri.EscapeDataString(matricula)}&order=data.asc",
                    ct);
                alteracoesCargo = cargos
                    .Select(c => new AlteracaoCargoDto(
                        FormatDate(c.Data),
                        c.Cargo ?? "",
                        c.Motivo,
                        c.CboEsocial))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Histórico de cargo não disponível para {Matricula}.", matricula);
            }

            // 7. Registros de férias
            var ferias = new List<FeriasDto>();
            try
            {
                var feriasRows = await _sb.GetAsync<FeriasRow>(
                    "pag_ferias",
                    $"funcionario=eq.{Uri.EscapeDataString(matricula)}&order=per_ini_aq.asc",
                    ct);
                ferias = feriasRows
                    .Select(f => new FeriasDto(
                        FormatPeriod(f.PerIniAq, f.PerFimAq),
                        FormatPeriod(f.DatIniGo, f.DatFimGo),
                        f.DiasGozo,
                        f.DiasAbono,
                        f.DiasLicenca))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Férias não disponíveis para {Matricula}.", matricula);
            }

            // 8. Registros de afastamento
            var afastamentos = new List<AfastamentoDto>();
            try
            {
                var afRows = await _sb.GetAsync<AfastamentoRow>(
                    "pag_afastamento",
                    $"funcionario=eq.{Uri.EscapeDataString(matricula)}&order=data_ini.asc",
                    ct);
                afastamentos = afRows
                    .Select(a => new AfastamentoDto(
                        FormatPeriod(a.DataIni, a.DataFim),
                        a.Motivo,
                        a.QtdDias))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Afastamentos não disponíveis para {Matricula}.", matricula);
            }

            // Merge: view_func → pag_funcionario → fat_destinatario_pfisica
            string? Merge(string? a, string? b, string? c = null)
                => NullIfEmpty(a) ?? NullIfEmpty(b) ?? NullIfEmpty(c);

            return new FichaRegistroDto(
                empresa,
                cnpjEmpresa,
                endEmpresa,
                bairroEmpresa,
                munEmpresa,
                ufEmpresa,
                cnaeEmpresa,
                matricula,
                func.Nome ?? pagFunc?.Nome ?? "",
                FormatCpf(Merge(func.Cpf, pagFunc?.Cpf, pfisica?.Cpf) ?? ""),
                Merge(func.Pis, pagFunc?.Pis),
                FormatDate(Merge(func.Nascimento, pagFunc?.Nascimento, pfisica?.Nascimento)),
                Merge(func.Sexo, pagFunc?.Sexo, pfisica?.Sexo),
                Merge(func.EstadoCivil, pagFunc?.EstadoCivil, pfisica?.EstadoCivil),
                Merge(func.Nacionalidade, pagFunc?.Nacionalidade, pfisica?.Nacionalidade),
                Merge(func.NomeMae, pagFunc?.NomeMae, pfisica?.NomeMae),
                Merge(func.NomePai, pagFunc?.NomePai, pfisica?.NomePai),
                Merge(func.GrauInstrucao, pagFunc?.GrauInstrucao, pfisica?.GrauInstrucao),
                Merge(func.Endereco, pagFunc?.Endereco, pfisica?.Endereco),
                Merge(func.Bairro, pagFunc?.Bairro, pfisica?.Bairro),
                Merge(func.Cep, pagFunc?.Cep, pfisica?.Cep),
                Merge(func.Municipio, pagFunc?.Municipio, pfisica?.Municipio),
                Merge(func.Uf, pagFunc?.Uf, pfisica?.Uf),
                Merge(func.Rg, pagFunc?.Rg, pfisica?.Rg),
                Merge(func.RgOrgao, pagFunc?.RgOrgao, pfisica?.RgOrgao),
                FormatDate(Merge(func.RgExpedicao, pagFunc?.RgExpedicao, pfisica?.RgExpedicao)),
                Merge(func.RgUf, pagFunc?.RgUf, pfisica?.RgUf),
                Merge(func.Ctps, pagFunc?.Ctps, pfisica?.Ctps),
                Merge(func.CtpsSerie, pagFunc?.CtpsSerie, pfisica?.CtpsSerie),
                FormatDate(Merge(func.CtpsExpedicao, pagFunc?.CtpsExpedicao, pfisica?.CtpsExpedicao)),
                Merge(func.CtpsUf, pagFunc?.CtpsUf, pfisica?.CtpsUf),
                func.Cargo,
                func.CboEsocial,
                func.Departamento ?? func.Diretoria,
                func.Setor,
                func.Admissao,
                FormatDate(func.Desligamento),
                func.Salario,
                func.HorasMensais?.ToString(),
                Merge(func.Sindicato, pagFunc?.Sindicato),
                Merge(func.FormaRemuneracao, pagFunc?.FormaRemuneracao),
                func.EmailContato,
                celular,
                alteracoesCargo,
                alteracoesSalariais,
                ferias,
                afastamentos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao montar ficha de registro para {Matricula}.", matricula);
            return null;
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static string? NullIfEmpty(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s;

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

    private static string FormatDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        if (DateTime.TryParse(raw, out var dt))
            return dt.ToString("dd/MM/yyyy");
        return raw;
    }

    private static string FormatPeriod(string? ini, string? fim)
    {
        var s = FormatDate(ini);
        var e = FormatDate(fim);
        if (string.IsNullOrEmpty(s)) return "";
        return string.IsNullOrEmpty(e) ? s : $"{s} à {e}";
    }

    // ─── Row records ────────────────────────────────────────────────────────────

    private record ViewFuncFullRow(
        [property: JsonPropertyName("funcionario")] string? Funcionario,
        [property: JsonPropertyName("nome")] string? Nome,
        [property: JsonPropertyName("cpf")] string? Cpf,
        [property: JsonPropertyName("pis")] string? Pis,
        [property: JsonPropertyName("departamento")] string? Departamento,
        [property: JsonPropertyName("diretoria")] string? Diretoria,
        [property: JsonPropertyName("setor")] string? Setor,
        [property: JsonPropertyName("cargo")] string? Cargo,
        [property: JsonPropertyName("admissao")] string? Admissao,
        [property: JsonPropertyName("filialdestinatario")] string? FilialDestinatario,
        [property: JsonPropertyName("emailcontato")] string? EmailContato,
        [property: JsonPropertyName("nascimento")] string? Nascimento,
        [property: JsonPropertyName("sexo")] string? Sexo,
        [property: JsonPropertyName("estadocivil")] string? EstadoCivil,
        [property: JsonPropertyName("nacionalidade")] string? Nacionalidade,
        [property: JsonPropertyName("nomemae")] string? NomeMae,
        [property: JsonPropertyName("nomepai")] string? NomePai,
        [property: JsonPropertyName("grauinstrucao")] string? GrauInstrucao,
        [property: JsonPropertyName("endereco")] string? Endereco,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("cep")] string? Cep,
        [property: JsonPropertyName("municipio")] string? Municipio,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("rg")] string? Rg,
        [property: JsonPropertyName("rgorgao")] string? RgOrgao,
        [property: JsonPropertyName("rgexpedicao")] string? RgExpedicao,
        [property: JsonPropertyName("rguf")] string? RgUf,
        [property: JsonPropertyName("ctps")] string? Ctps,
        [property: JsonPropertyName("ctpsserie")] string? CtpsSerie,
        [property: JsonPropertyName("ctpsexpedicao")] string? CtpsExpedicao,
        [property: JsonPropertyName("ctpsuf")] string? CtpsUf,
        [property: JsonPropertyName("desligamento")] string? Desligamento,
        [property: JsonPropertyName("salario")] decimal? Salario,
        [property: JsonPropertyName("horasmensais")] int? HorasMensais,
        [property: JsonPropertyName("sindicato")] string? Sindicato,
        [property: JsonPropertyName("formaremuneracao")] string? FormaRemuneracao,
        [property: JsonPropertyName("cboesocial")] string? CboEsocial
    );

    private record ViewDeptoFullRow(
        [property: JsonPropertyName("destinatario")] string? Destinatario,
        [property: JsonPropertyName("nome")] string? Nome,
        [property: JsonPropertyName("cgccpf")] string? CgcCpf,
        [property: JsonPropertyName("endereco")] string? Endereco,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("municipio")] string? Municipio,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("cnae")] string? Cnae
    );

    private record AcessoRow(
        [property: JsonPropertyName("matricula")] string? Matricula,
        [property: JsonPropertyName("celular")] string? Celular
    );

    private record MovimentoSalarioRow(
        [property: JsonPropertyName("funcionario")] string Funcionario,
        [property: JsonPropertyName("mes")] int Mes,
        [property: JsonPropertyName("ano")] int Ano,
        [property: JsonPropertyName("valor")] decimal Valor
    );

    private record CargoHistRow(
        [property: JsonPropertyName("funcionario")] string? Funcionario,
        [property: JsonPropertyName("data")] string? Data,
        [property: JsonPropertyName("cargo")] string? Cargo,
        [property: JsonPropertyName("motivo")] string? Motivo,
        [property: JsonPropertyName("cboesocial")] string? CboEsocial
    );

    private record FeriasRow(
        [property: JsonPropertyName("funcionario")] string? Funcionario,
        [property: JsonPropertyName("per_ini_aq")] string? PerIniAq,
        [property: JsonPropertyName("per_fim_aq")] string? PerFimAq,
        [property: JsonPropertyName("dat_ini_go")] string? DatIniGo,
        [property: JsonPropertyName("dat_fim_go")] string? DatFimGo,
        [property: JsonPropertyName("dias_gozo")] int? DiasGozo,
        [property: JsonPropertyName("dias_abono")] int? DiasAbono,
        [property: JsonPropertyName("dias_licenca")] int? DiasLicenca
    );

    private record AfastamentoRow(
        [property: JsonPropertyName("funcionario")] string? Funcionario,
        [property: JsonPropertyName("data_ini")] string? DataIni,
        [property: JsonPropertyName("data_fim")] string? DataFim,
        [property: JsonPropertyName("motivo")] string? Motivo,
        [property: JsonPropertyName("qtd_dias")] int? QtdDias
    );

    // Dados pessoais diretos da tabela pag_funcionario
    private record PagFuncionarioRow(
        [property: JsonPropertyName("funcionario")] string? Funcionario,
        [property: JsonPropertyName("nome")] string? Nome,
        [property: JsonPropertyName("cpf")] string? Cpf,
        [property: JsonPropertyName("pis")] string? Pis,
        [property: JsonPropertyName("nascimento")] string? Nascimento,
        [property: JsonPropertyName("sexo")] string? Sexo,
        [property: JsonPropertyName("estadocivil")] string? EstadoCivil,
        [property: JsonPropertyName("nacionalidade")] string? Nacionalidade,
        [property: JsonPropertyName("nomemae")] string? NomeMae,
        [property: JsonPropertyName("nomepai")] string? NomePai,
        [property: JsonPropertyName("grauinstrucao")] string? GrauInstrucao,
        [property: JsonPropertyName("endereco")] string? Endereco,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("cep")] string? Cep,
        [property: JsonPropertyName("municipio")] string? Municipio,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("rg")] string? Rg,
        [property: JsonPropertyName("rgorgao")] string? RgOrgao,
        [property: JsonPropertyName("rgexpedicao")] string? RgExpedicao,
        [property: JsonPropertyName("rguf")] string? RgUf,
        [property: JsonPropertyName("ctps")] string? Ctps,
        [property: JsonPropertyName("ctpsserie")] string? CtpsSerie,
        [property: JsonPropertyName("ctpsexpedicao")] string? CtpsExpedicao,
        [property: JsonPropertyName("ctpsuf")] string? CtpsUf,
        [property: JsonPropertyName("sindicato")] string? Sindicato,
        [property: JsonPropertyName("formaremuneracao")] string? FormaRemuneracao
    );

    // Dados pessoais complementares de fat_destinatario_pfisica
    private record FatPfisicaRow(
        [property: JsonPropertyName("funcionario")] string? Funcionario,
        [property: JsonPropertyName("cpf")] string? Cpf,
        [property: JsonPropertyName("nascimento")] string? Nascimento,
        [property: JsonPropertyName("sexo")] string? Sexo,
        [property: JsonPropertyName("estadocivil")] string? EstadoCivil,
        [property: JsonPropertyName("nacionalidade")] string? Nacionalidade,
        [property: JsonPropertyName("nomemae")] string? NomeMae,
        [property: JsonPropertyName("nomepai")] string? NomePai,
        [property: JsonPropertyName("grauinstrucao")] string? GrauInstrucao,
        [property: JsonPropertyName("endereco")] string? Endereco,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("cep")] string? Cep,
        [property: JsonPropertyName("municipio")] string? Municipio,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("rg")] string? Rg,
        [property: JsonPropertyName("rgorgao")] string? RgOrgao,
        [property: JsonPropertyName("rgexpedicao")] string? RgExpedicao,
        [property: JsonPropertyName("rguf")] string? RgUf,
        [property: JsonPropertyName("ctps")] string? Ctps,
        [property: JsonPropertyName("ctpsserie")] string? CtpsSerie,
        [property: JsonPropertyName("ctpsexpedicao")] string? CtpsExpedicao,
        [property: JsonPropertyName("ctpsuf")] string? CtpsUf
    );
}
