using System.Text.Json;

namespace PainelControlador.Api.Dtos;

public record SqlConsultaRequest(string Ambiente, string Sql);

/// <summary>Cada linha é a lista de valores na ordem de Colunas: objeto JSON perderia colunas de nome repetido (select 1, 2; join com dois id).</summary>
public record SqlResultado(List<string> Colunas, List<List<JsonElement>> Linhas, bool Truncado, long DuracaoMs);
