using FluentAssertions;
using MediCore.Application.Abstractions;
using MediCore.Application.Patients;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Application.UnitTests;

public class CoverageHistoryTests
{
    [Fact]
    public async Task Add_first_active_coverage_becomes_active()
    {
        var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var handler = new AddCoverage.Handler(context, new TestCurrentUserService());

        var result = await handler.HandleAsync(new AddCoverage.Command(
            patientId,
            "ObraSocial",
            Guid.NewGuid(),
            "Plan Base",
            "123456"));

        result.IsSuccess.Should().BeTrue();

        var entry = await context.CoverageEntries.FindAsync(result.Value);
        entry.Should().NotBeNull();
        entry!.IsActive.Should().BeTrue();
        entry.Type.Should().Be(CoverageType.ObraSocial);
    }

    [Fact]
    public async Task Add_second_active_coverage_deactivates_previous()
    {
        var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var insurerId = Guid.NewGuid();
        var handler = new AddCoverage.Handler(context, new TestCurrentUserService());

        var first = await handler.HandleAsync(new AddCoverage.Command(
            patientId,
            "ObraSocial",
            insurerId,
            "Plan Base",
            "123456"));
        first.IsSuccess.Should().BeTrue();

        var second = await handler.HandleAsync(new AddCoverage.Command(
            patientId,
            "Prepaga",
            Guid.NewGuid(),
            "Plan Premium",
            "999888"));
        second.IsSuccess.Should().BeTrue();

        var entries = await context.CoverageEntries
            .Where(c => c.PatientId == patientId)
            .ToListAsync();

        entries.Should().HaveCount(2);
        entries.Should().ContainSingle(c => c.IsActive);
        entries.Single(c => c.IsActive).Id.Should().Be(second.Value);
        entries.Should().ContainSingle(c => !c.IsActive && c.Id == first.Value);
    }

    [Fact]
    public async Task Add_particular_coverage_without_insurer_or_affiliate_succeeds()
    {
        var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var handler = new AddCoverage.Handler(context, new TestCurrentUserService());

        var result = await handler.HandleAsync(new AddCoverage.Command(
            patientId,
            "Particular",
            null,
            "Self-pay",
            null));

        result.IsSuccess.Should().BeTrue();

        var entry = await context.CoverageEntries.FindAsync(result.Value);
        entry.Should().NotBeNull();
        entry!.Type.Should().Be(CoverageType.Particular);
        entry.InsurerId.Should().BeNull();
        entry.AffiliateNumber.Should().BeNull();
        entry.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Add_obra_social_without_insurer_is_rejected()
    {
        var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var handler = new AddCoverage.Handler(context, new TestCurrentUserService());

        var result = await handler.HandleAsync(new AddCoverage.Command(
            patientId,
            "ObraSocial",
            null,
            "Plan Base",
            "123456"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Coverage.InsurerRequired");
    }

    [Fact]
    public async Task Add_obra_social_without_affiliate_number_is_rejected()
    {
        var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var handler = new AddCoverage.Handler(context, new TestCurrentUserService());

        var result = await handler.HandleAsync(new AddCoverage.Command(
            patientId,
            "ObraSocial",
            Guid.NewGuid(),
            "Plan Base",
            null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Coverage.AffiliateNumberRequired");
    }

    [Fact]
    public async Task Add_particular_with_insurer_is_rejected()
    {
        var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var handler = new AddCoverage.Handler(context, new TestCurrentUserService());

        var result = await handler.HandleAsync(new AddCoverage.Command(
            patientId,
            "Particular",
            Guid.NewGuid(),
            "Self-pay",
            null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Coverage.InsurerNotAllowed");
    }

    private static AppDbContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<Guid> CreatePatientAsync(AppDbContext context)
    {
        var handler = new CreatePatient.Handler(context, new TestCurrentUserService());
        var result = await handler.HandleAsync(new CreatePatient.Command(
            "DNI",
            Guid.NewGuid().ToString(),
            "Maria",
            "Gonzalez",
            new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero),
            new CreatePatient.ContactRequest(
                MobilePhone: "+54 9 11 1234-5678",
                LandlinePhone: null,
                Email: null),
            new CreatePatient.AddressRequest(
                Street: "Av. Santa Fe",
                Number: "1234",
                City: "Buenos Aires",
                Province: "CABA",
                PostalCode: "C1059"),
            null));

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string? UserName => "tester";
        public IReadOnlyList<string> Roles => ["Receptionist"];
    }
}
