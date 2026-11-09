using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Infrastructure.Identity;
using MediCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace MediCore.Api.IntegrationTests;

/// <summary>
/// The demo accounts carry a published password, so the development gate in the host is the
/// only thing keeping them out of a non-development deployment. These tests pin that gate,
/// the password policy the published credential relies on, and the specialty link that makes
/// the seeded professional bookable.
/// </summary>
public class DemoDataSeederGuardTests : IntegrationTestBase
{
    private static readonly Guid DentistrySpecialtyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task A_non_development_host_never_creates_the_demo_accounts()
    {
        MsSqlContainer container;
        try
        {
            container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithCleanUp(true)
                .Build();
            await container.StartAsync();
        }
        catch (Exception ex) when (ex.GetType().FullName?.Contains("DockerUnavailable") == true)
        {
            Skip.If(true, "SQL Server container is required to prove the non-development gate");
            return;
        }

        await using var _ = container;
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                $"{container.GetConnectionString()};Connect Timeout=60");
        });

        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = DemoDataSeeder.ProfessionalEmail, password = DemoDataSeeder.Password });

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await context.Users.CountAsync(u => u.Email!.EndsWith("@medicore.local"))).Should().Be(0);
        (await context.Professionals
                .CountAsync(p => p.NationalLicenseMN == DemoDataSeeder.ProfessionalLicenseMN))
            .Should().Be(0);
    }

    [Fact]
    public async Task The_demo_seed_creates_exactly_three_accounts_with_one_role_each()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the demo seed tests");

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var demoUsers = await context.Users
            .Where(u => u.Email!.EndsWith("@medicore.local"))
            .ToListAsync();

        demoUsers.Should().HaveCount(3);
        demoUsers.Select(u => u.Email).Should().BeEquivalentTo(
            DemoDataSeeder.AdminEmail,
            DemoDataSeeder.ReceptionistEmail,
            DemoDataSeeder.ProfessionalEmail);

        foreach (var demoUser in demoUsers)
        {
            (await userManager.GetRolesAsync(demoUser)).Should().HaveCount(1);
        }

        (await context.Professionals
                .CountAsync(p => p.NationalLicenseMN == DemoDataSeeder.ProfessionalLicenseMN))
            .Should().Be(1);
    }

    [Fact]
    public async Task The_published_demo_password_satisfies_the_configured_identity_policy()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the password policy test");

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var probe = new ApplicationUser
        {
            UserName = "policy-probe@ceibos.demo",
            Email = "policy-probe@ceibos.demo"
        };

        userManager.PasswordValidators.Should().NotBeEmpty();

        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, probe, DemoDataSeeder.Password);
            result.Succeeded.Should().BeTrue(
                because: $"the published demo password must satisfy every configured rule ({string.Join("; ", result.Errors.Select(e => e.Description))})");
        }
    }

    [Fact]
    public async Task The_demo_professional_rejects_a_specialty_it_does_not_hold()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the booking guard test");

        using var seedScope = Factory.Services.CreateScope();
        var context = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var professionalId = await context.Professionals
            .Where(p => p.NationalLicenseMN == DemoDataSeeder.ProfessionalLicenseMN)
            .Select(p => p.Id)
            .SingleAsync();

        var heldSpecialtyIds = await context.ProfessionalSpecialties
            .Where(ps => ps.ProfessionalId == professionalId)
            .Select(ps => ps.SpecialtyId)
            .ToListAsync();

        heldSpecialtyIds.Should().NotBeEmpty();
        heldSpecialtyIds.Should().NotContain(DentistrySpecialtyId);

        using var client = Factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = DemoDataSeeder.AdminEmail, password = DemoDataSeeder.Password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var patient = await client.PostAsJsonAsync("/api/patients", new
        {
            documentType = "DNI",
            documentNumber = "40111222",
            firstName = "Maria",
            lastName = "Gonzalez",
            birthDate = new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero),
            contact = new
            {
                mobilePhone = "+54 9 11 1234-5678",
                landlinePhone = (string?)null,
                email = (string?)null
            },
            address = new
            {
                street = "Av. Santa Fe",
                number = "1234",
                city = "Buenos Aires",
                province = "CABA",
                postalCode = "C1059"
            }
        });
        patient.EnsureSuccessStatusCode();
        var patientId = (await patient.Content.ReadFromJsonAsync<CreatePatientResponse>())!.PatientId;

        var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(3).AddHours(10), TimeSpan.Zero);

        var rejected = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = DentistrySpecialtyId,
            start,
            isSobreturno = false
        });
        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await rejected.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.SpecialtyNotHeld");

        var accepted = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = heldSpecialtyIds[0],
            start,
            isSobreturno = false
        });
        accepted.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private sealed record LoginResponse(Guid UserId, string Email, string AccessToken, DateTimeOffset ExpiresAt);

    private sealed record CreatePatientResponse(Guid PatientId);

    private sealed record ErrorResponse(string Code, string Message);
}
