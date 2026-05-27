using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public class AuthService
{
    private readonly SupabaseRestClient _sb;
    private readonly JwtOptions _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(SupabaseRestClient sb, JwtOptions jwt, ILogger<AuthService> logger)
    {
        _sb = sb;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        try
        {
            var users = await _sb.GetAsync<PainelUser>(
                "painel_users",
                $"username=eq.{Uri.EscapeDataString(request.Username)}&is_active=eq.true",
                ct);

            var user = users.FirstOrDefault();
            if (user is null)
            {
                _logger.LogWarning("Login falhou — usuário '{Username}' não encontrado.", request.Username);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login falhou — senha incorreta para '{Username}'.", request.Username);
                return null;
            }

            var token = GenerateToken(user.Username);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.ExpiresInMinutes);

            _logger.LogInformation("Login bem-sucedido para '{Username}'.", user.Username);
            return new LoginResponse(token, user.Username, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao autenticar usuário '{Username}'.", request.Username);
            return null;
        }
    }

    public async Task<(bool ok, string? error)> ChangePasswordAsync(
        string username, ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return (false, "A nova senha e a confirmação não coincidem.");

        try
        {
            var users = await _sb.GetAsync<PainelUser>(
                "painel_users",
                $"username=eq.{Uri.EscapeDataString(username)}&is_active=eq.true",
                ct);

            var user = users.FirstOrDefault();
            if (user is null) return (false, "Usuário não encontrado.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return (false, "Senha atual incorreta.");

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 10);
            await _sb.PatchAsync(
                "painel_users",
                $"username=eq.{Uri.EscapeDataString(username)}",
                new { password_hash = newHash },
                ct);

            _logger.LogInformation("Senha alterada para '{Username}'.", username);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar senha de '{Username}'.", username);
            return (false, "Erro interno ao alterar a senha.");
        }
    }

    private string GenerateToken(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, username),
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiresInMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private record PainelUser(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password_hash")] string PasswordHash,
        [property: JsonPropertyName("is_active")] bool IsActive
    );
}
