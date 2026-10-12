using FluentAssertions;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

public class InsurerSeedTests
{
    [Fact]
    public async Task Insurer_seed_is_idempotent_and_creates_three_fictional_insurers()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("InsurerSeed"));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        await DbSeeder.SeedInsurersAsync(context);
        await DbSeeder.SeedInsurersAsync(context);

        var insurers = await context.Insurers.ToListAsync();
        insurers.Should().HaveCount(3);
        insurers.Should().OnlyHaveUniqueItems(i => i.Name);
        insurers.Should().ContainSingle(i => i.Type == InsurerType.Prepaga);
        insurers.Should().ContainSingle(i => i.Name == "Sanatorio Los Ceibos Salud");
    }
}
