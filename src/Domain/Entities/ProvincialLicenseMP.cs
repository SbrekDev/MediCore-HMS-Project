namespace MediCore.Domain.Entities;

public class ProvincialLicenseMP
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
}
