using System.Reflection;
using FluentAssertions;
using MediCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

public class RoleSeedTests
{
    [Fact]
    public async Task Role_seed_creates_Admin_Receptionist_Professional()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("RoleSeed"));
        services.AddDbContext<TestIdentityDbContext>(options => options.UseInMemoryDatabase("RoleSeed"));
        services.AddIdentityCore<IdentityUser<Guid>>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TestIdentityDbContext>();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        await using var identityDb = scope.ServiceProvider.GetRequiredService<TestIdentityDbContext>();
        await identityDb.Database.EnsureCreatedAsync();

        var seederType = Type.GetType(
            "MediCore.Infrastructure.Persistence.DbSeeder, MediCore.Infrastructure",
            throwOnError: false);

        seederType.Should().NotBeNull(
            "Role seeder (DbSeeder) must be implemented in Infrastructure");

        var seedMethod = seederType!.GetMethod(
            "SeedRolesAsync",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
            null,
            new[] { typeof(IServiceProvider) },
            null);

        seedMethod.Should().NotBeNull(
            "DbSeeder must expose a public SeedRolesAsync(IServiceProvider) method");

        if (seedMethod!.IsStatic)
        {
            await (Task)seedMethod.Invoke(null, new object[] { scope.ServiceProvider })!;
        }
        else
        {
            var instance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, seederType);
            await (Task)seedMethod.Invoke(instance, new object[] { scope.ServiceProvider })!;
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        string[] expectedRoles = ["Admin", "Receptionist", "Professional"];

        foreach (var role in expectedRoles)
        {
            var exists = await roleManager.RoleExistsAsync(role);
            exists.Should().BeTrue($"Role '{role}' should exist after seeding");
        }
    }
}

public class TestIdentityDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public TestIdentityDbContext(DbContextOptions<TestIdentityDbContext> options)
        : base(options)
    {
    }
}
