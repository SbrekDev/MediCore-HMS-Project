using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace MediCore.Api.IntegrationTests;

public class AuthTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public AuthTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Failed_login_returns_401()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for auth integration tests");

        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@ceibos.demo",
            password = "WrongPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_request_to_protected_endpoint_returns_401()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for auth integration tests");

        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Successful_login_returns_jwt_with_role_claims_and_httpOnly_refresh_cookie()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for auth integration tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "receptionist@ceibos.demo",
            password = "TestPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);
        token.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == result.UserId.ToString());
        token.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Receptionist");

        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.ToList()
            : new List<string>();

        foreach (var cookie in cookies)
        {
            _output.WriteLine($"Set-Cookie: {cookie}");
        }

        cookies.Should().ContainSingle(c =>
            c.Contains("refreshToken", StringComparison.OrdinalIgnoreCase) &&
            c.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
            c.Contains("path=/api/auth", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record LoginResponse(
        Guid UserId,
        string Email,
        string AccessToken,
        DateTimeOffset ExpiresAt);
}
