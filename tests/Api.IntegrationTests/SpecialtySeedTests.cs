using FluentAssertions;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

public class SpecialtySeedTests
{
    [Fact]
    public async Task Specialty_seed_is_idempotent_and_creates_thirty_minute_defaults()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("SpecialtySeed"));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        await DbSeeder.SeedSpecialtiesAsync(context);
        await DbSeeder.SeedSpecialtiesAsync(context);

        var specialties = await context.Specialties.ToListAsync();
        specialties.Should().NotBeEmpty();
        specialties.Should().OnlyHaveUniqueItems(s => s.Code);
        specialties.Should().AllSatisfy(s => s.DefaultDurationMinutes.Should().Be(30));
    }
}
