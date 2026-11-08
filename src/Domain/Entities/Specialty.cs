using MediCore.Domain.Common;

namespace MediCore.Domain.Entities;

public class Specialty : AuditableEntity<Guid>
{
    public Specialty(Guid id) : base(id)
    {
    }

    public Specialty() : base(default)
    {
    }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DefaultDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}
