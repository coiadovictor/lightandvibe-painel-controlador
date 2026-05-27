using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Dtos;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _auth.LoginAsync(request, ct);
        if (result is null)
            return Unauthorized(new { error = "Usuário ou senha inválidos." });

        return Ok(result);
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var (ok, error) = await _auth.ChangePasswordAsync(username, request, ct);
        if (!ok) return BadRequest(new { error });

        return Ok(new { message = "Senha alterada com sucesso." });
    }
}
