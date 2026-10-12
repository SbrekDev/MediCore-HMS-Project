using FluentAssertions;
using MediCore.Application.Abstractions;
using MediCore.Application.Patients;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Application.UnitTests;

public class PatientRegistrationTests
{
    [Fact]
    public async Task Register_patient_captures_document_names_birth_date_contact_and_address()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("PatientRegistration"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new CreatePatient.Handler(context, new TestCurrentUserService());
        var result = await handler.HandleAsync(new CreatePatient.Command(
            "DNI",
            "30111222",
            "Maria",
            "Gonzalez",
            new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero),
            new CreatePatient.ContactRequest(
                MobilePhone: "+54 9 11 1234-5678",
                LandlinePhone: "+54 11 8765-4321",
                Email: "maria.gonzalez@example.com"),
            new CreatePatient.AddressRequest(
                Street: "Av. Santa Fe",
                Number: "1234",
                City: "Buenos Aires",
                Province: "CABA",
                PostalCode: "C1059"),
            null));

        result.IsSuccess.Should().BeTrue();

        var patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == result.Value);

        patient.Should().NotBeNull();
        patient!.DocumentType.Should().Be("DNI");
        patient.DocumentNumber.Should().Be("30111222");
        patient.FirstName.Should().Be("Maria");
        patient.LastName.Should().Be("Gonzalez");
        patient.BirthDate.Should().Be(new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero));
        patient.Contact.MobilePhone.Should().Be("+54 9 11 1234-5678");
        patient.Contact.LandlinePhone.Should().Be("+54 11 8765-4321");
        patient.Contact.Email.Should().Be("maria.gonzalez@example.com");
        patient.Address.Street.Should().Be("Av. Santa Fe");
        patient.Address.Number.Should().Be("1234");
        patient.Address.City.Should().Be("Buenos Aires");
        patient.Address.Province.Should().Be("CABA");
        patient.Address.PostalCode.Should().Be("C1059");
    }

    [Fact]
    public async Task Register_duplicate_document_type_and_number_returns_conflict()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("PatientDuplicate"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new CreatePatient.Handler(context, new TestCurrentUserService());
        await handler.HandleAsync(CreateCommand("DNI", "30111222"));

        var result = await handler.HandleAsync(CreateCommand("DNI", "30111222"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Patient.DuplicateDocument");
    }

    [Fact]
    public async Task Register_same_document_number_with_different_type_is_accepted()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("PatientDifferentType"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new CreatePatient.Handler(context, new TestCurrentUserService());
        await handler.HandleAsync(CreateCommand("DNI", "30111222"));

        var result = await handler.HandleAsync(CreateCommand("Pasaporte", "30111222"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Register_minor_without_responsible_is_rejected()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("PatientMinorNoResponsible"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new CreatePatient.Handler(context, new TestCurrentUserService());
        var result = await handler.HandleAsync(new CreatePatient.Command(
            "DNI",
            "40111222",
            "Child",
            "Gonzalez",
            DateTimeOffset.UtcNow.AddYears(-10),
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

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Patient.ResponsibleRequired");
    }

    [Fact]
    public async Task Register_minor_with_existing_responsible_is_accepted()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("PatientMinorWithResponsible"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new CreatePatient.Handler(context, new TestCurrentUserService());
        var responsibleResult = await handler.HandleAsync(CreateCommand("DNI", "50111222"));
        responsibleResult.IsSuccess.Should().BeTrue();

        var result = await handler.HandleAsync(new CreatePatient.Command(
            "DNI",
            "60111222",
            "Kid",
            "Gonzalez",
            DateTimeOffset.UtcNow.AddYears(-10),
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
            responsibleResult.Value));

        result.IsSuccess.Should().BeTrue();

        var patient = await context.Patients.FindAsync(result.Value);
        patient.Should().NotBeNull();
        patient!.ResponsiblePatientId.Should().Be(responsibleResult.Value);
    }

    [Fact]
    public async Task Register_adult_without_responsible_is_accepted()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("PatientAdultNoResponsible"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new CreatePatient.Handler(context, new TestCurrentUserService());
        var result = await handler.HandleAsync(new CreatePatient.Command(
            "DNI",
            "70111222",
            "Adult",
            "Gonzalez",
            DateTimeOffset.UtcNow.AddYears(-25),
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
    }

    private static CreatePatient.Command CreateCommand(string documentType, string documentNumber)
    {
        return new CreatePatient.Command(
            documentType,
            documentNumber,
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
            null);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string? UserName => "tester";
        public IReadOnlyList<string> Roles => ["Receptionist"];
    }
}
