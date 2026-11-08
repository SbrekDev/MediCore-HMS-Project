using MediCore.Domain.Common;
using MediCore.Domain.Enums;

namespace MediCore.Domain.Entities;

public class Appointment : AuditableEntity<Guid>
{
    public Appointment(Guid id) : base(id)
    {
    }

    public Appointment() : base(default)
    {
    }

    public Guid PatientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid SpecialtyId { get; set; }
    public DateTimeOffset StartDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    public bool IsSobreturno { get; set; }
    public bool IsLateCancellation { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
