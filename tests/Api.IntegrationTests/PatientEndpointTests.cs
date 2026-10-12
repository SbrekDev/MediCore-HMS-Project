using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

public class PatientEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task Post_patient_captures_contact_and_address()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "30111222"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreatePatientResponse>();
        result!.PatientId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Post_duplicate_document_returns_conflict()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "30111222"));
        var response = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "30111222"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Patient.DuplicateDocument");
    }

    [Fact]
    public async Task Post_same_number_different_document_type_is_accepted()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var first = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "30111222"));
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("Pasaporte", "30111222"));
        second.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Get_patients_returns_list()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "30111222"));

        var getResponse = await client.GetAsync("/api/patients");
        getResponse.EnsureSuccessStatusCode();

        var patients = await getResponse.Content.ReadFromJsonAsync<List<PatientDto>>();
        patients.Should().ContainSingle(p => p.DocumentNumber == "30111222");
    }

    [Fact]
    public async Task Get_patient_by_id_returns_patient()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var create = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "30111222"));
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatePatientResponse>();

        var getResponse = await client.GetAsync($"/api/patients/{created!.PatientId}");
        getResponse.EnsureSuccessStatusCode();

        var patient = await getResponse.Content.ReadFromJsonAsync<PatientDto>();
        patient!.Id.Should().Be(created.PatientId);
        patient.Contact.MobilePhone.Should().Be("+54 9 11 1234-5678");
        patient.Address.City.Should().Be("Buenos Aires");
    }

    [Fact]
    public async Task Put_patient_updates_contact_and_address()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var create = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "30111222"));
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatePatientResponse>();

        var update = new
        {
            firstName = "Maria Elena",
            lastName = "Gonzalez",
            birthDate = new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero),
            contact = new
            {
                mobilePhone = "+54 9 11 9999-8888",
                landlinePhone = (string?)null,
                email = (string?)null
            },
            address = new
            {
                street = "Calle Nueva",
                number = "555",
                city = "Córdoba",
                province = "Córdoba",
                postalCode = "X5000"
            }
        };

        var putResponse = await client.PutAsJsonAsync($"/api/patients/{created!.PatientId}", update);
        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/patients/{created.PatientId}");
        getResponse.EnsureSuccessStatusCode();
        var patient = await getResponse.Content.ReadFromJsonAsync<PatientDto>();
        patient!.FirstName.Should().Be("Maria Elena");
        patient.Contact.MobilePhone.Should().Be("+54 9 11 9999-8888");
        patient.Address.City.Should().Be("Córdoba");
    }

    [Fact]
    public async Task Post_minor_without_responsible_returns_bad_request()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest(
            "DNI",
            "40111222",
            birthDate: DateTimeOffset.UtcNow.AddYears(-10)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Patient.ResponsibleRequired");
    }

    [Fact]
    public async Task Post_minor_with_responsible_returns_created()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var responsible = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "50111222"));
        responsible.EnsureSuccessStatusCode();
        var responsiblePatient = await responsible.Content.ReadFromJsonAsync<CreatePatientResponse>();

        var response = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest(
            "DNI",
            "60111222",
            birthDate: DateTimeOffset.UtcNow.AddYears(-10),
            responsiblePatientId: responsiblePatient!.PatientId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_coverage_becomes_active()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var create = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "80111222"));
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatePatientResponse>();

            var response = await client.PostAsJsonAsync(
            $"/api/patients/{created!.PatientId}/coverages",
            BuildCoverageRequest("ObraSocial", Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"), "123456"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_second_coverage_deactivates_first()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var create = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "90111222"));
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatePatientResponse>();

        var first = await client.PostAsJsonAsync(
            $"/api/patients/{created!.PatientId}/coverages",
            BuildCoverageRequest("ObraSocial", Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"), "123456"));
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync(
            $"/api/patients/{created.PatientId}/coverages",
            BuildCoverageRequest("Prepaga", Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"), "999888"));
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entries = await dbContext.CoverageEntries
            .Where(c => c.PatientId == created.PatientId)
            .ToListAsync();

        entries.Should().HaveCount(2);
        entries.Should().ContainSingle(c => c.IsActive);
    }

    [Fact]
    public async Task Post_particular_coverage_without_insurer_succeeds()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var create = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "11111111"));
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatePatientResponse>();

        var response = await client.PostAsJsonAsync(
            $"/api/patients/{created!.PatientId}/coverages",
            new
            {
                type = "Particular",
                planName = "Self-pay"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_obra_social_coverage_without_insurer_returns_bad_request()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for patient endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var create = await client.PostAsJsonAsync("/api/patients", BuildCreateRequest("DNI", "22222222"));
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatePatientResponse>();

        var response = await client.PostAsJsonAsync(
            $"/api/patients/{created!.PatientId}/coverages",
            new
            {
                type = "ObraSocial",
                planName = "Plan Base"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Coverage.InsurerRequired");
    }

    private static object BuildCreateRequest(
        string documentType,
        string documentNumber,
        DateTimeOffset? birthDate = null,
        Guid? responsiblePatientId = null)
    {
        return new
        {
            documentType,
            documentNumber,
            firstName = "Maria",
            lastName = "Gonzalez",
            birthDate = birthDate ?? new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero),
            contact = new
            {
                mobilePhone = "+54 9 11 1234-5678",
                landlinePhone = "+54 11 8765-4321",
                email = "maria.gonzalez@example.com"
            },
            address = new
            {
                street = "Av. Santa Fe",
                number = "1234",
                city = "Buenos Aires",
                province = "CABA",
                postalCode = "C1059"
            },
            responsiblePatientId
        };
    }

    private static object BuildCoverageRequest(string type, Guid insurerId, string affiliateNumber)
    {
        return new
        {
            type,
            insurerId,
            planName = "Plan Base",
            affiliateNumber
        };
    }

    private sealed record CreatePatientResponse(Guid PatientId);

    private sealed record ErrorResponse(string Code, string Message);

    private sealed record PatientDto(
        Guid Id,
        string DocumentType,
        string DocumentNumber,
        string FirstName,
        string LastName,
        DateTimeOffset BirthDate,
        ContactDto Contact,
        AddressDto Address);

    private sealed record ContactDto(string MobilePhone, string? LandlinePhone, string? Email);

    private sealed record AddressDto(string Street, string Number, string City, string Province, string PostalCode);
}
