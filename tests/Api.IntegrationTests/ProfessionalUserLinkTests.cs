using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Infrastructure.Identity;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

/// <summary>
/// The complete/no-show ownership guard trusts <see cref="ApplicationUser.ProfessionalId"/>,
/// so the link between a user account and a professional must keep database integrity even
/// though no endpoint assigns it yet (seeding is the only writer today).
/// </summary>
public class ProfessionalUserLinkTests : IntegrationTestBase
{
    [Fact]
    public async Task Linking_two_users_to_the_same_professional_is_rejected()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for professional user link tests");

        var professionalId = await CreateProfessionalAsync();

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "link-a@ceibos.demo",
            ProfessionalId = professionalId
        });
        await dbContext.SaveChangesAsync();

        dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "link-b@ceibos.demo",
            ProfessionalId = professionalId
        });

        var act = async () => await dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Linking_a_user_to_an_unknown_professional_is_rejected()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for professional user link tests");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "link-unknown@ceibos.demo",
            ProfessionalId = Guid.NewGuid()
        });

        var act = async () => await dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Linking_a_user_to_a_professional_is_accepted()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for professional user link tests");

        var professionalId = await CreateProfessionalAsync();

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "link-valid@ceibos.demo",
            ProfessionalId = professionalId
        };
        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        var persisted = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        persisted.ProfessionalId.Should().Be(professionalId);
    }

    private async Task<Guid> CreateProfessionalAsync()
    {
        var client = Factory.CreateClient();

        await SeedUserAsync("admin-link@ceibos.demo", "TestPass123!", "Admin");
        var adminToken = await LoginAsync(client, "admin-link@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.PostAsJsonAsync("/api/professionals", new
        {
            firstName = "Lucia",
            lastName = "Ferrari",
            nationalLicenseMN = "LINK001MN",
            provincialLicenses = Array.Empty<object>()
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateProfessionalResponse>();
        return result!.ProfessionalId;
    }

    private sealed record CreateProfessionalResponse(Guid ProfessionalId);
}
