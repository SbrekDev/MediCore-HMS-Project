using MediCore.Domain.Common;

namespace MediCore.Domain.Entities;

public class Insurer : AuditableEntity<Guid>
{
    public Insurer(Guid id) : base(id)
    {
    }

    public Insurer() : base(default)
    {
    }

    public string Name { get; set; } = string.Empty;
    public InsurerType Type { get; set; }
}

public enum InsurerType
{
    ObraSocial,
    Prepaga
}
