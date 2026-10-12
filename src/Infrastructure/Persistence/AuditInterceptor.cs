using System.Text.Json;
using MediCore.Application.Abstractions;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MediCore.Infrastructure.Persistence;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    private static readonly HashSet<string> AuditedEntityTypes = new()
    {
        typeof(Patient).FullName!
    };

    public AuditInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext context)
        {
            await CaptureChangesAsync(context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task CaptureChangesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity.GetType().FullName is { } fullName &&
                        AuditedEntityTypes.Contains(fullName) &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => "Changed"
            };

            var entityId = GetEntityId(entry);
            var changesJson = BuildChangesJson(entry);

            var auditEntry = new AuditEntry
            {
                Id = Guid.NewGuid(),
                ActorUserId = _currentUserService.UserId,
                Action = action,
                EntityType = entry.Entity.GetType().Name,
                EntityId = entityId,
                TimestampUtc = DateTimeOffset.UtcNow,
                ChangesJson = changesJson
            };

            context.AuditEntries.Add(auditEntry);
        }

        await Task.CompletedTask;
    }

    private static Guid GetEntityId(EntityEntry entry)
    {
        var idProperty = entry.Property("Id");
        var idValue = idProperty.CurrentValue;

        if (idValue is Guid guid)
        {
            return guid;
        }

        return Guid.Empty;
    }

    private static string? BuildChangesJson(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (entry.State == EntityState.Added)
            {
                changes[property.Metadata.Name] = property.CurrentValue;
            }
            else if (entry.State == EntityState.Deleted)
            {
                changes[property.Metadata.Name] = property.OriginalValue;
            }
            else if (entry.State == EntityState.Modified && property.IsModified)
            {
                changes[property.Metadata.Name] = new
                {
                    From = property.OriginalValue,
                    To = property.CurrentValue
                };
            }
        }

        return changes.Count > 0
            ? JsonSerializer.Serialize(changes)
            : null;
    }
}
