using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using MediCore.Api.Endpoints;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Infrastructure.Identity;
using MediCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

/// <summary>
/// The demo seed exists because nothing else links an <see cref="ApplicationUser"/> to a
/// professional, which is what the complete/no-show ownership guard depends on. These tests
/// pin the two consequences that matter: the link is real, and it reaches the issued token.
/// </summary>
public class DemoDataSeederTests : IntegrationTestBase
{
    [Fact]
    public async Task Demo_seed_links_the_professional_user_to_a_bookable_professional()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the demo seed tests");

        await DemoDataSeeder.SeedAsync(Factory.Services);

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var professionalUser = await userManager.FindByEmailAsync(DemoDataSeeder.ProfessionalEmail);
        professionalUser.Should().NotBeNull();
        professionalUser!.ProfessionalId.Should().NotBeNull();
        (await userManager.IsInRoleAsync(professionalUser, "Professional")).Should().BeTrue();

        var professional = await context.Professionals
            .Include(p => p.Specialties)
            .SingleAsync(p => p.Id == professionalUser.ProfessionalId);

        professional.NationalLicenseMN.Should().Be(DemoDataSeeder.ProfessionalLicenseMN);
        // Without a held specialty every booking for this professional is rejected, so the
        // walkthrough would stop at the first appointment.
        professional.Specialties.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Demo_seed_creates_the_three_roles_and_is_idempotent()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the demo seed tests");

        await DemoDataSeeder.SeedAsync(Factory.Services);
        await DemoDataSeeder.SeedAsync(Factory.Services);

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await userManager.FindByEmailAsync(DemoDataSeeder.AdminEmail))!.Should().NotBeNull();
        (await userManager.FindByEmailAsync(DemoDataSeeder.ReceptionistEmail))!.Should().NotBeNull();
        (await userManager.FindByEmailAsync(DemoDataSeeder.ProfessionalEmail))!.Should().NotBeNull();

        var licenses = await context.Professionals
            .CountAsync(p => p.NationalLicenseMN == DemoDataSeeder.ProfessionalLicenseMN);
        licenses.Should().Be(1);
    }

    [Fact]
    public async Task The_professional_demo_login_issues_the_professional_claim()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the demo seed tests");

        await DemoDataSeeder.SeedAsync(Factory.Services);

        using var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(DemoDataSeeder.ProfessionalEmail, DemoDataSeeder.Password));

        response.IsSuccessStatusCode.Should().BeTrue();

        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("accessToken").GetString();
        token.Should().NotBeNullOrWhiteSpace();

        var payload = DecodePayload(token!);
        payload.Should().Contain("professional_id");
    }

    [Fact]
    public async Task Demo_seed_repairs_a_missing_link_and_a_missing_specialty()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the demo seed tests");

        Guid professionalId;

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await userManager.FindByEmailAsync(DemoDataSeeder.ProfessionalEmail);
            professionalId = user!.ProfessionalId!.Value;

            // Simulate a database seeded before the link existed: an account with no link and a
            // professional with no specialty. Both make the account silently unusable.
            user.ProfessionalId = null;
            (await userManager.UpdateAsync(user)).Succeeded.Should().BeTrue();

            await context.ProfessionalSpecialties
                .Where(ps => ps.ProfessionalId == professionalId)
                .ExecuteDeleteAsync();
        }

        await DemoDataSeeder.SeedAsync(Factory.Services);

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var repairedUser = await userManager.FindByEmailAsync(DemoDataSeeder.ProfessionalEmail);
            repairedUser!.ProfessionalId.Should().Be(professionalId);

            var links = await context.ProfessionalSpecialties
                .CountAsync(ps => ps.ProfessionalId == professionalId);
            links.Should().BeGreaterThan(0);
        }
    }

    private static string DecodePayload(string token)
    {
        var parts = token.Split('.');
        parts.Should().HaveCount(3);

        var padded = parts[1].Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - (padded.Length % 4)) % 4, '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
