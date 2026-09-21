using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

public class AuditTests : IntegrationTestBase
{
    [Fact]
    public async Task Patient_mutation_writes_AuditEntry()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for audit integration tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var accessToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            documentType = "DNI",
            documentNumber = "12345678",
            firstName = "Maria",
            lastName = "Gonzalez",
            birthDate = new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero)
        });

        response.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEntry = await context.AuditEntries.FirstOrDefaultAsync();

        auditEntry.Should().NotBeNull();
        auditEntry!.Action.Should().Be("Created");
        auditEntry.EntityType.Should().Be("Patient");
        auditEntry.ActorUserId.Should().NotBeNull();
        auditEntry.ChangesJson.Should().NotBeNullOrWhiteSpace();
        auditEntry.TimestampUtc.Should().BeWithin(TimeSpan.FromMinutes(1)).Before(DateTimeOffset.UtcNow);
    }
}
