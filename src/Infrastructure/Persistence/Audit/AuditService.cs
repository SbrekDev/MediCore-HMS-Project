using MediCore.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

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
        // Inserted with SQL rather than through the ChangeTracker: SaveChanges would also
        // commit whatever else the caller happens to have pending, which turns an audit call
        // into a silent partial commit of somebody else's unit of work. The insert still joins
        // the caller's ambient transaction, which is what keeps the audit row atomic with the
        // change it describes.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [AuditEntries] ([Id], [ActorUserId], [Action], [EntityType], [EntityId], [TimestampUtc], [ChangesJson])
            VALUES ({Guid.NewGuid()}, {actorId}, {action}, {entityType}, {entityId}, {timestampUtc}, {changesJson})
            """,
            cancellationToken);
    }
}
