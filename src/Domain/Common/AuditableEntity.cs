namespace MediCore.Domain.Common;

public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : struct
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public Guid ModifiedBy { get; set; }

    protected AuditableEntity(TId id) : base(id)
    {
    }

    protected AuditableEntity()
        : base(default)
    {
    }
}
