using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Domain.Entities;
using MediCore.Domain.Enums;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

public class DashboardEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task Get_summary_returns_counts_for_seeded_data_in_range()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for dashboard endpoint tests");

        var (client, _, range) = await SeedSummaryScenarioAsync();

        var response = await client.GetAsync($"/api/dashboard/summary?from={Uri.EscapeDataString(range.From)}&to={Uri.EscapeDataString(range.To)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();
        result.Should().NotBeNull();
        result!.From.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?-03:00$");
        result.To.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?-03:00$");
        DateTimeOffset.Parse(result.From).Should().Be(DateTimeOffset.Parse(range.From));
        DateTimeOffset.Parse(result.To).Should().Be(DateTimeOffset.Parse(range.To));
        result.Appointments.Total.Should().Be(4);
        result.Appointments.Confirmed.Should().Be(1);
        result.Appointments.Completed.Should().Be(1);
        result.Appointments.NoShow.Should().Be(1);
        result.Appointments.Cancelled.Should().Be(1);
        result.Appointments.Sobreturnos.Should().Be(1);
        result.Patients.Total.Should().Be(3);
        result.Patients.AddedInRange.Should().Be(2);
        result.Professionals.Total.Should().Be(3);
        result.Professionals.WithTemplate.Should().Be(1);
    }

    [Fact]
    public async Task Get_summary_excluding_appointments_returns_zero_appointment_counts_but_global_totals()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for dashboard endpoint tests");

        var (client, _, range) = await SeedSummaryScenarioAsync();
        var laterFrom = range.OutsideFrom;
        var laterTo = range.OutsideTo;

        var response = await client.GetAsync($"/api/dashboard/summary?from={Uri.EscapeDataString(laterFrom)}&to={Uri.EscapeDataString(laterTo)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();
        result.Should().NotBeNull();
        result!.Appointments.Total.Should().Be(0);
        result.Appointments.Confirmed.Should().Be(0);
        result.Appointments.Completed.Should().Be(0);
        result.Appointments.NoShow.Should().Be(0);
        result.Appointments.Cancelled.Should().Be(0);
        result.Appointments.Sobreturnos.Should().Be(0);
        result.Patients.Total.Should().Be(3);
        result.Patients.AddedInRange.Should().Be(0);
        result.Professionals.Total.Should().Be(3);
        result.Professionals.WithTemplate.Should().Be(1);
    }

    [Theory]
    [InlineData("2026-11-12T00:00:00", "2026-11-13T00:00:00-03:00")]
    [InlineData("2026-11-12T00:00:00-03:00", "2026-11-13T00:00:00")]
    public async Task Get_summary_with_offsetless_bound_returns_range_required(string from, string to)
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for dashboard endpoint tests");

        var (client, _, _) = await SeedSummaryScenarioAsync();

        var response = await client.GetAsync($"/api/dashboard/summary?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Dashboard.RangeRequired");
    }

    [Theory]
    [InlineData("2026-11-13T00:00:00-03:00", "2026-11-12T00:00:00-03:00")]
    [InlineData("2026-11-12T00:00:00-03:00", "2026-11-12T00:00:00-03:00")]
    public async Task Get_summary_with_inverted_or_empty_range_returns_invalid_range(string from, string to)
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for dashboard endpoint tests");

        var (client, _, _) = await SeedSummaryScenarioAsync();

        var response = await client.GetAsync($"/api/dashboard/summary?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Dashboard.InvalidRange");
    }

    [Theory]
    [InlineData("admin-dash@ceibos.demo", "Admin", HttpStatusCode.OK)]
    [InlineData("receptionist-dash@ceibos.demo", "Receptionist", HttpStatusCode.OK)]
    [InlineData("professional-dash@ceibos.demo", "Professional", HttpStatusCode.OK)]
    public async Task Get_summary_enforces_clinical_staff_policy(string email, string role, HttpStatusCode expectedStatus)
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for dashboard endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync(email, "TestPass123!", role);
        var accessToken = await LoginAsync(client, email, "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync($"/api/dashboard/summary?from={Uri.EscapeDataString("2026-11-12T00:00:00-03:00")}&to={Uri.EscapeDataString("2026-11-13T00:00:00-03:00")}");

        response.StatusCode.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task Get_summary_by_anonymous_user_returns_unauthorized()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for dashboard endpoint tests");

        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/summary?from=2026-11-12T00:00:00-03:00&to=2026-11-13T00:00:00-03:00");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(HttpClient Client, string AdminToken, SummaryRange Range)> SeedSummaryScenarioAsync()
    {
        var client = Factory.CreateClient();

        await SeedUserAsync("admin-dash@ceibos.demo", "TestPass123!", "Admin");
        var adminToken = await LoginAsync(client, "admin-dash@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var from = new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.FromHours(-3));
        var to = new DateTimeOffset(2026, 11, 13, 0, 0, 0, TimeSpan.FromHours(-3));
        var outsideFrom = new DateTimeOffset(2026, 11, 13, 0, 0, 0, TimeSpan.FromHours(-3));
        var outsideTo = new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.FromHours(-3));

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var specialtyId = Guid.NewGuid();
        dbContext.Specialties.Add(new Specialty(specialtyId)
        {
            Name = "Cardiology",
            DefaultDurationMinutes = 30
        });

        var professionalWithTemplate = new Professional(Guid.NewGuid())
        {
            FirstName = "Ana",
            LastName = "Demo",
            NationalLicenseMN = $"MN-DASH-{Guid.NewGuid():N}"
        };
        var professionalWithoutTemplate = new Professional(Guid.NewGuid())
        {
            FirstName = "Juan",
            LastName = "Perez",
            NationalLicenseMN = $"MN-DASH-{Guid.NewGuid():N}"
        };
        dbContext.Professionals.AddRange(professionalWithTemplate, professionalWithoutTemplate);

        var agenda = new Agenda(Guid.NewGuid())
        {
            ProfessionalId = professionalWithTemplate.Id,
            MaxSobreturnosPerDay = 2,
            Blocks = new List<AvailabilityBlock>
            {
                new AvailabilityBlock
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = DayOfWeek.Monday,
                    Start = new TimeOnly(8, 0),
                    End = new TimeOnly(12, 0)
                }
            }
        };
        dbContext.Agendas.Add(agenda);

        var patientInRange = new Patient(Guid.NewGuid())
        {
            DocumentType = "DNI",
            DocumentNumber = "11111111",
            FirstName = "Maria",
            LastName = "Gonzalez",
            BirthDate = new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero),
            CreatedAt = from.AddHours(2),
            CreatedBy = Guid.Empty,
            ModifiedAt = from.AddHours(2),
            ModifiedBy = Guid.Empty
        };
        var patientAlsoInRange = new Patient(Guid.NewGuid())
        {
            DocumentType = "DNI",
            DocumentNumber = "22222222",
            FirstName = "Lucia",
            LastName = "Fernandez",
            BirthDate = new DateTimeOffset(1990, 3, 10, 0, 0, 0, TimeSpan.Zero),
            CreatedAt = from.AddHours(5),
            CreatedBy = Guid.Empty,
            ModifiedAt = from.AddHours(5),
            ModifiedBy = Guid.Empty
        };
        var patientOutsideRange = new Patient(Guid.NewGuid())
        {
            DocumentType = "DNI",
            DocumentNumber = "33333333",
            FirstName = "Jorge",
            LastName = "Prueba",
            BirthDate = new DateTimeOffset(1978, 8, 20, 0, 0, 0, TimeSpan.Zero),
            CreatedAt = outsideTo.AddHours(2),
            CreatedBy = Guid.Empty,
            ModifiedAt = outsideFrom.AddHours(2),
            ModifiedBy = Guid.Empty
        };
        dbContext.Patients.AddRange(patientInRange, patientAlsoInRange, patientOutsideRange);

        dbContext.Appointments.AddRange(
            new Appointment(Guid.NewGuid())
            {
                PatientId = patientInRange.Id,
                ProfessionalId = professionalWithTemplate.Id,
                SpecialtyId = specialtyId,
                StartDateTime = from.AddHours(8),
                DurationMinutes = 30,
                Status = AppointmentStatus.Confirmed,
                IsSobreturno = false,
                CreatedAt = from.AddHours(1),
                CreatedBy = Guid.Empty,
                ModifiedAt = from.AddHours(1),
                ModifiedBy = Guid.Empty
            },
            new Appointment(Guid.NewGuid())
            {
                PatientId = patientAlsoInRange.Id,
                ProfessionalId = professionalWithTemplate.Id,
                SpecialtyId = specialtyId,
                StartDateTime = from.AddHours(9),
                DurationMinutes = 30,
                Status = AppointmentStatus.Completed,
                IsSobreturno = false,
                CreatedAt = from.AddHours(1),
                CreatedBy = Guid.Empty,
                ModifiedAt = from.AddHours(1),
                ModifiedBy = Guid.Empty
            },
            new Appointment(Guid.NewGuid())
            {
                PatientId = patientOutsideRange.Id,
                ProfessionalId = professionalWithTemplate.Id,
                SpecialtyId = specialtyId,
                StartDateTime = from.AddHours(10),
                DurationMinutes = 30,
                Status = AppointmentStatus.NoShow,
                IsSobreturno = true,
                CreatedAt = from.AddHours(1),
                CreatedBy = Guid.Empty,
                ModifiedAt = from.AddHours(1),
                ModifiedBy = Guid.Empty
            },
            new Appointment(Guid.NewGuid())
            {
                PatientId = patientInRange.Id,
                ProfessionalId = professionalWithoutTemplate.Id,
                SpecialtyId = specialtyId,
                StartDateTime = from.AddHours(11),
                DurationMinutes = 30,
                Status = AppointmentStatus.Cancelled,
                IsSobreturno = false,
                IsLateCancellation = true,
                CreatedAt = from.AddHours(1),
                CreatedBy = Guid.Empty,
                ModifiedAt = from.AddHours(1),
                ModifiedBy = Guid.Empty
            });

        await dbContext.SaveChangesAsync();

        return (client, adminToken, new SummaryRange(from.ToString("O"), to.ToString("O"), outsideFrom.ToString("O"), outsideTo.ToString("O")));
    }

    private sealed record SummaryRange(string From, string To, string OutsideFrom, string OutsideTo);

    private sealed record DashboardSummaryResponse(
        string From,
        string To,
        AppointmentCounts Appointments,
        PatientCounts Patients,
        ProfessionalCounts Professionals);

    private sealed record AppointmentCounts(
        int Total,
        int Confirmed,
        int Completed,
        int NoShow,
        int Cancelled,
        int Sobreturnos);

    private sealed record PatientCounts(int Total, int AddedInRange);

    private sealed record ProfessionalCounts(int Total, int WithTemplate);

    private sealed record ErrorResponse(string Code, string Message);
}
