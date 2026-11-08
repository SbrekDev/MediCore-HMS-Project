using MediCore.Domain.Common;

namespace MediCore.Domain.Entities;

public class Professional : AuditableEntity<Guid>
{
    public Professional(Guid id) : base(id)
    {
    }

    public Professional() : base(default)
    {
    }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string NationalLicenseMN { get; set; } = string.Empty;
    public ICollection<ProvincialLicenseMP> ProvincialLicenses { get; set; } = new List<ProvincialLicenseMP>();
    public ICollection<ProfessionalSpecialty> Specialties { get; set; } = new List<ProfessionalSpecialty>();
}
