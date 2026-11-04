using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Dtos;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

/// <summary>Console SQL: consultas de leitura no banco do RH (produção ou homolog).</summary>
[Authorize]
[ApiController]
[Route("api/sql")]
public class SqlController : ControllerBase
{
    private static readonly string[] Ambientes = ["producao", "homolog"];
    private readonly ISqlService _svc;
    public SqlController(ISqlService svc) => _svc = svc;

    [HttpPost("consultar")]
    public async Task<IActionResult> Consultar([FromBody] SqlConsultaRequest req, CancellationToken ct)
    {
        if (req is null || !Ambientes.Contains(req.Ambiente))
            return BadRequest(new { message = "Ambiente deve ser 'producao' ou 'homolog'." });
        if (string.IsNullOrWhiteSpace(req.Sql))
            return BadRequest(new { message = "Consulta vazia." });
        try
        {
            return Ok(await _svc.ConsultarAsync(req.Ambiente, req.Sql, ct));
        }
        catch (SqlConsultaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
