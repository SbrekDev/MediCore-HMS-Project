namespace MediCore.Application.Abstractions;

public interface IAuditService
{
    Task CaptureAsync(
        string action,
        string entityType,
        Guid entityId,
        Guid? actorId,
        DateTimeOffset timestampUtc,
        string? changesJson,
        CancellationToken cancellationToken = default);
}
