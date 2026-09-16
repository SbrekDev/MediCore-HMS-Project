using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediCore.Api.IntegrationTests;

public class DatabaseContainerTests : IntegrationTestBase
{
    [SkippableFact]
    public async Task AppDbContext_can_connect_to_testcontainers_sql_server()
    {
        Skip.If(!ContainerAvailable, "Docker is not available; skipping Testcontainers smoke test.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var canConnect = await context.Database.CanConnectAsync();

        canConnect.Should().BeTrue("AppDbContext should be able to connect to the Testcontainers SQL Server instance");
    }
}
