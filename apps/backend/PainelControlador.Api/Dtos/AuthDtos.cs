using System.ComponentModel.DataAnnotations;

namespace PainelControlador.Api.Dtos;

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record LoginResponse(
    string Token,
    string Username,
    DateTime ExpiresAt
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword,
    [Required] string ConfirmPassword
);
