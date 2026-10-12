namespace MediCore.Domain.Entities;

public class AgendaException
{
    public Guid Id { get; set; }
    public Guid AgendaId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
    public string Reason { get; set; } = string.Empty;

    public bool IsFullDay => Start is null || End is null;

    public bool IsBlocking(TimeOnly time)
    {
        if (IsFullDay)
        {
            return true;
        }

        return time >= Start! && time < End!;
    }
}
