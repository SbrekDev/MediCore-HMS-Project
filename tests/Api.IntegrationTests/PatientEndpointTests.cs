using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;

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

    private static object BuildCreateRequest(string documentType, string documentNumber)
    {
        return new
        {
            documentType,
            documentNumber,
            firstName = "Maria",
            lastName = "Gonzalez",
            birthDate = new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero),
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
            }
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
