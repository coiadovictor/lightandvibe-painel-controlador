using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/trocas-numero")]
public class TrocasNumeroController : ControllerBase
{
    private readonly ITrocaNumeroService _svc;
    public TrocasNumeroController(ITrocaNumeroService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await _svc.ListarPendentesAsync(ct));

    [HttpGet("pendentes/count")]
    public async Task<IActionResult> Contar(CancellationToken ct) => Ok(new { total = await _svc.ContarPendentesAsync(ct) });

    [HttpPost("{id:long}/autorizar")]
    public async Task<IActionResult> Autorizar(long id, CancellationToken ct)
    {
        var r = await _svc.AutorizarAsync(id, Usuario(), ct);
        return r.Ok ? Ok(new { ok = true, sessaoAntigaInvalidada = r.SessaoAntigaInvalidada }) : Conflict(new { ok = false });
    }

    [HttpPost("{id:long}/recusar")]
    public async Task<IActionResult> Recusar(long id, CancellationToken ct)
    {
        var r = await _svc.RecusarAsync(id, Usuario(), ct);
        return r.Ok ? Ok(new { ok = true, sessaoAntigaInvalidada = false }) : Conflict(new { ok = false });
    }

    private string Usuario() => User.FindFirstValue(ClaimTypes.Name) ?? "desconhecido";
}
