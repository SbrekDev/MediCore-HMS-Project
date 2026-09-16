using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;

namespace MediCore.Api.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected bool ContainerAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();

            await _sqlContainer.StartAsync();
            ContainerAvailable = true;

            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development");
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),
                        });
                    });
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
}
