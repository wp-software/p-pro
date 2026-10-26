using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using PartnershipManager.Infrastructure.Services;

namespace PartnershipManager.Tests.Unit.Infrastructure;

public class AuthServiceSecurityTests
{
    [Fact]
    public void VerifyPassword_DoesNotWritePasswordOrHashToConsole()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-only-secret-with-at-least-32-characters"
            })
            .Build();
        var authService = new AuthService(configuration);
        const string password = "sensitive-test-password";
        var passwordHash = authService.HashPassword(password);
        var originalOutput = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);

            authService.VerifyPassword(password, passwordHash).Should().BeTrue();

            output.ToString().Should().NotContain(password);
            output.ToString().Should().NotContain(passwordHash);
            output.ToString().Should().BeEmpty();
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

    [Fact]
    public void GenerateJwtToken_ExpiresAfterFiveMinutesByDefault()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-only-secret-with-at-least-32-characters"
            })
            .Build();
        var authService = new AuthService(configuration);
        var user = User.Create(Guid.NewGuid(), "token@example.com", "Token User", "hash");
        var now = DateTime.UtcNow;

        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            authService.GenerateJwtToken(user, Array.Empty<string>()));

        token.ValidTo.Should().BeAfter(now.AddMinutes(4));
        token.ValidTo.Should().BeBefore(now.AddMinutes(6));
    }
}
