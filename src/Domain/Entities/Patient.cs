using MediCore.Domain.Common;

namespace MediCore.Domain.Entities;

public class Patient : AuditableEntity<Guid>
{
    public Patient(Guid id) : base(id)
    {
    }

    public Patient() : base(default)
    {
    }

    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTimeOffset BirthDate { get; set; }

    public Guid? ResponsiblePatientId { get; set; }
    public Patient? ResponsiblePatient { get; set; }

    public Contact Contact { get; set; } = new Contact();
    public Address Address { get; set; } = new Address();

    public ICollection<CoverageEntry> CoverageEntries { get; set; } = new List<CoverageEntry>();
}
