using System.Text.Json;

namespace PainelControlador.Api.Dtos;

public record SqlConsultaRequest(string Ambiente, string Sql);

public record SqlResultado(List<string> Colunas, List<JsonElement> Linhas, bool Truncado, long DuracaoMs);
