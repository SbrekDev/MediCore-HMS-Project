using MediCore.Domain.Common;

namespace MediCore.Domain.Entities;

public class CoverageEntry : AuditableEntity<Guid>
{
    public CoverageEntry(Guid id) : base(id)
    {
    }

    public CoverageEntry() : base(default)
    {
    }

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public CoverageType Type { get; set; }
    public Guid? InsurerId { get; set; }
    public Insurer? Insurer { get; set; }

    public string PlanName { get; set; } = string.Empty;
    public string? AffiliateNumber { get; set; }
    public bool IsActive { get; set; }
}

public enum CoverageType
{
    ObraSocial,
    Prepaga,
    Particular
}
