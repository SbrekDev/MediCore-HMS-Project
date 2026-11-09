using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Application.Abstractions;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

/// <summary>
/// <see cref="IAuditService"/> exists for changes EF cannot see in the ChangeTracker. It is
/// registered application-wide and shares the scoped <see cref="AppDbContext"/>, so capturing
/// an audit row must not become a way to persist whatever else a caller happens to have
/// pending: that would commit another operation's half-finished work as a side effect.
/// </summary>
public class AuditServiceScopeTests : IntegrationTestBase
{
    [Fact]
    public async Task Capturing_an_audit_row_does_not_persist_unrelated_pending_changes()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the audit scope test");

        var auditTargetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

            // Tracked but deliberately not saved: this belongs to somebody else's unit of work.
            context.Specialties.Add(new Specialty(Guid.NewGuid())
            {
                Code = "PROBE-UNSAVED",
                Name = "Probe unsaved specialty",
                DefaultDurationMinutes = 15,
                IsActive = false,
                CreatedAt = now,
                CreatedBy = Guid.Empty,
                ModifiedAt = now,
                ModifiedBy = Guid.Empty
            });

            await auditService.CaptureAsync(
                "Created",
                "Patient",
                auditTargetId,
                actorId: null,
                timestampUtc: now,
                changesJson: "{\"Probe\":true}");
        }

        using var verificationScope = Factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var auditWritten = await verificationContext.AuditEntries
            .AnyAsync(e => e.EntityId == auditTargetId && e.EntityType == "Patient");
        auditWritten.Should().BeTrue("the audit row is the whole point of the call");

        var specialtyLeaked = await verificationContext.Specialties
            .AnyAsync(s => s.Code == "PROBE-UNSAVED");
        specialtyLeaked.Should().BeFalse("auditing must not commit another unit of work");
    }
}
