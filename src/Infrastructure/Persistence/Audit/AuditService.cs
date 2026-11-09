using MediCore.Application.Abstractions;

namespace MediCore.Infrastructure.Persistence.Audit;

/// <summary>
/// Explicit audit capture for changes EF cannot see in the ChangeTracker, such as an atomic
/// UPDATE issued through <c>ExecuteUpdateAsync</c>.
/// </summary>
public class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task CaptureAsync(
        string action,
        string entityType,
        Guid entityId,
        Guid? actorId,
        DateTimeOffset timestampUtc,
        string? changesJson,
        CancellationToken cancellationToken = default)
    {
        _context.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            TimestampUtc = timestampUtc,
            ChangesJson = changesJson
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
