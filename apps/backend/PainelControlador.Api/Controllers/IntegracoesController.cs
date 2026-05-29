using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IntegracoesController : ControllerBase
{
    private readonly IIntegracoesService _service;

    public IntegracoesController(IIntegracoesService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? situacao,
        [FromQuery] string? tabela,
        [FromQuery] int limit = 200,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(situacao, tabela, limit, ct);
        return Ok(result);
    }
}
