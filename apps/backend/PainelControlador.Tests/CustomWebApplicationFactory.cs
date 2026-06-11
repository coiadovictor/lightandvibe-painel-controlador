using Microsoft.AspNetCore.Mvc.Testing;

namespace PainelControlador.Tests;

/// <summary>
/// Sobe a API com a configuração mínima obrigatória para testes de integração.
/// A app é uma API protegida por JWT e lê Jwt:Secret avidamente em tempo de builder
/// (antes do Build), montando a chave de assinatura HMAC. Sem secret, o handler de
/// autenticação lança em toda requisição (inclusive nas anônimas), retornando 500.
///
/// Como a leitura é ávida, overrides via ConfigureAppConfiguration (aplicados só no
/// Build) não chegam a tempo — por isso injetamos o secret como variável de ambiente
/// do processo, definida ANTES de o entry point (Program) rodar. CreateBuilder lê o
/// provider de env vars no início, então o valor é capturado corretamente.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public CustomWebApplicationFactory()
    {
        // Jwt:Secret -> env var Jwt__Secret. >= 32 bytes para satisfazer HMAC-SHA256.
        Environment.SetEnvironmentVariable(
            "Jwt__Secret", "test-only-secret-please-change-0123456789abcdef");
    }
}
