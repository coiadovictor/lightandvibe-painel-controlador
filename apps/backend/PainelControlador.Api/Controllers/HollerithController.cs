using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HollerithController : ControllerBase
{
    private readonly IHollerithService _service;

    public HollerithController(IHollerithService service) => _service = service;

    [HttpGet("{matricula}")]
    public async Task<IActionResult> Get(
        string matricula,
        [FromQuery] int mes,
        [FromQuery] int ano,
        CancellationToken ct)
    {
        if (mes < 1 || mes > 12)
            return BadRequest(new { error = "Mês inválido." });

        var minAno = 2020;
        var now = DateTime.UtcNow;
        if (ano < minAno || ano > now.Year)
            return BadRequest(new { error = $"Ano deve ser entre {minAno} e {now.Year}." });
        if (ano == now.Year && mes > now.Month)
            return BadRequest(new { error = "Período ainda não disponível." });

        var result = await _service.GetAsync(matricula, mes, ano, ct);
        if (result is null)
            return NotFound(new { error = "Nenhum movimento encontrado para o período informado." });

        return Ok(result);
    }
}
