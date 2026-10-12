using FluentAssertions;
using MediCore.Application.Abstractions;
using MediCore.Application.Professionals;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Application.UnitTests;

public class ProfessionalRegistrationTests
{
    [Fact]
    public async Task Register_professional_with_MN_and_two_MPs_persists_all_licenses()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("ProfessionalRegistration"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new CreateProfessional.Handler(context, new TestCurrentUserService());
        var result = await handler.HandleAsync(new CreateProfessional.Command(
            "John",
            "Doe",
            "12345",
            [
                new CreateProfessional.ProvincialLicenseRequest("MP-001", "Buenos Aires"),
                new CreateProfessional.ProvincialLicenseRequest("MP-002", "Córdoba")
            ]));

        result.IsSuccess.Should().BeTrue();
        var professional = await context.Professionals
            .Include(p => p.ProvincialLicenses)
            .FirstOrDefaultAsync(p => p.Id == result.Value);

        professional.Should().NotBeNull();
        professional!.FirstName.Should().Be("John");
        professional.LastName.Should().Be("Doe");
        professional.NationalLicenseMN.Should().Be("12345");
        professional.ProvincialLicenses.Should().HaveCount(2);
        professional.ProvincialLicenses.Should().ContainSingle(mp => mp.Number == "MP-001" && mp.Province == "Buenos Aires");
        professional.ProvincialLicenses.Should().ContainSingle(mp => mp.Number == "MP-002" && mp.Province == "Córdoba");
    }

    [Fact]
    public async Task Register_professional_without_MN_is_rejected()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("ProfessionalRegistrationNoMn"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new CreateProfessional.Handler(context, new TestCurrentUserService());
        var result = await handler.HandleAsync(new CreateProfessional.Command(
            "John",
            "Doe",
            "",
            []));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Professional.NationalLicenseMNRequired");
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string? UserName => "tester";
        public IReadOnlyList<string> Roles => ["Admin"];
    }
}
