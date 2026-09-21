using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace MediCore.Api.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected bool ContainerAvailable { get; private set; }

    protected async Task SeedUserAsync(string email, string password, string role)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            await userManager.DeleteAsync(existing);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        createResult.Succeeded.Should().BeTrue();

        var addRoleResult = await userManager.AddToRoleAsync(user, role);
        addRoleResult.Succeeded.Should().BeTrue();
    }

    protected async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        return result.AccessToken;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithCleanUp(true)
                .Build();

            await _sqlContainer.StartAsync();
            ContainerAvailable = true;

            var connectionString = $"{_sqlContainer.GetConnectionString()};Connect Timeout=60";
            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development");
                    builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
                });
        }
        catch (Exception ex) when (ex.GetType().FullName?.Contains("DockerUnavailable") == true)
        {
            ContainerAvailable = false;
            Factory = new WebApplicationFactory<Program>();
        }
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();

        if (_sqlContainer is not null)
        {
            await _sqlContainer.DisposeAsync();
        }
    }

    private sealed record LoginResponse(
        Guid UserId,
        string Email,
        string AccessToken,
        DateTimeOffset ExpiresAt);
}
