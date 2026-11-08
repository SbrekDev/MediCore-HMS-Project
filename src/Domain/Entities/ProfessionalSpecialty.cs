namespace MediCore.Domain.Entities;

public class ProfessionalSpecialty
{
    public Guid ProfessionalId { get; set; }
    public Guid SpecialtyId { get; set; }
    public bool IsPrimary { get; set; }
    public int? DurationOverrideMinutes { get; set; }

    public int GetDurationMinutes(int specialtyDefaultDuration)
    {
        return DurationOverrideMinutes ?? specialtyDefaultDuration;
    }
}
