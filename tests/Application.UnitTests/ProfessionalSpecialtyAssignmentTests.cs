using FluentAssertions;
using MediCore.Application.Abstractions;
using MediCore.Application.Professionals;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Application.UnitTests;

public class ProfessionalSpecialtyAssignmentTests
{
    [Fact]
    public async Task Assign_multiple_specialties_records_primary_flag_on_chosen_one()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("ProfessionalSpecialtyAssignment"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var professional = new Professional(Guid.NewGuid())
        {
            FirstName = "John",
            LastName = "Doe",
            NationalLicenseMN = "12345",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = Guid.Empty
        };

        var cardiology = new Specialty(Guid.NewGuid()) { Code = "CARDIOLOGY", Name = "Cardiología", DefaultDurationMinutes = 30, IsActive = true };
        var generalMedicine = new Specialty(Guid.NewGuid()) { Code = "GENERAL_MEDICINE", Name = "Clínica Médica", DefaultDurationMinutes = 30, IsActive = true };

        context.Professionals.Add(professional);
        context.Specialties.Add(cardiology);
        context.Specialties.Add(generalMedicine);
        await context.SaveChangesAsync();

        var handler = new AssignProfessionalSpecialty.Handler(context, new TestCurrentUserService());

        var cardiologyResult = await handler.HandleAsync(new AssignProfessionalSpecialty.Command(
            professional.Id,
            cardiology.Id,
            IsPrimary: true));
        var generalMedicineResult = await handler.HandleAsync(new AssignProfessionalSpecialty.Command(
            professional.Id,
            generalMedicine.Id,
            IsPrimary: false));

        cardiologyResult.IsSuccess.Should().BeTrue();
        generalMedicineResult.IsSuccess.Should().BeTrue();

        var assignments = await context.ProfessionalSpecialties
            .Where(ps => ps.ProfessionalId == professional.Id)
            .ToListAsync();

        assignments.Should().HaveCount(2);
        assignments.Should().ContainSingle(ps => ps.SpecialtyId == cardiology.Id && ps.IsPrimary);
        assignments.Should().ContainSingle(ps => ps.SpecialtyId == generalMedicine.Id && !ps.IsPrimary);
    }

    [Fact]
    public async Task Assigning_a_new_primary_clears_previous_primary_flag()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("ProfessionalSpecialtyPrimarySwap"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var professional = new Professional(Guid.NewGuid())
        {
            FirstName = "John",
            LastName = "Doe",
            NationalLicenseMN = "12345",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = Guid.Empty
        };

        var cardiology = new Specialty(Guid.NewGuid()) { Code = "CARDIOLOGY", Name = "Cardiología", DefaultDurationMinutes = 30, IsActive = true };
        var generalMedicine = new Specialty(Guid.NewGuid()) { Code = "GENERAL_MEDICINE", Name = "Clínica Médica", DefaultDurationMinutes = 30, IsActive = true };

        context.Professionals.Add(professional);
        context.Specialties.Add(cardiology);
        context.Specialties.Add(generalMedicine);
        await context.SaveChangesAsync();

        var handler = new AssignProfessionalSpecialty.Handler(context, new TestCurrentUserService());

        await handler.HandleAsync(new AssignProfessionalSpecialty.Command(professional.Id, cardiology.Id, IsPrimary: true));
        await handler.HandleAsync(new AssignProfessionalSpecialty.Command(professional.Id, generalMedicine.Id, IsPrimary: true));

        var assignments = await context.ProfessionalSpecialties
            .Where(ps => ps.ProfessionalId == professional.Id)
            .ToListAsync();

        assignments.Should().ContainSingle(ps => ps.SpecialtyId == cardiology.Id && !ps.IsPrimary);
        assignments.Should().ContainSingle(ps => ps.SpecialtyId == generalMedicine.Id && ps.IsPrimary);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string? UserName => "tester";
        public IReadOnlyList<string> Roles => ["Admin"];
    }
}
