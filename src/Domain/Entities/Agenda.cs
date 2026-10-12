using MediCore.Domain.Common;

namespace MediCore.Domain.Entities;

public class Agenda : AuditableEntity<Guid>
{
    public Agenda(Guid id) : base(id)
    {
    }

    public Agenda() : base(default)
    {
    }

    public Guid ProfessionalId { get; set; }
    public int MaxSobreturnosPerDay { get; set; } = 0;
    public ICollection<AvailabilityBlock> Blocks { get; set; } = new List<AvailabilityBlock>();

    public bool CanAddSobreturno(int currentSobreturnosCount)
    {
        return currentSobreturnosCount < MaxSobreturnosPerDay;
    }
}
