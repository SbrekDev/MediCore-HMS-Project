namespace MediCore.Domain.Entities;

public class AvailabilityBlock
{
    public Guid Id { get; set; }
    public Guid AgendaId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }

    public bool Contains(TimeOnly time)
    {
        return time >= Start && time < End;
    }
}
