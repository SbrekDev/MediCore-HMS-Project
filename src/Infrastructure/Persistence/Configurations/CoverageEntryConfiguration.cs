using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class CoverageEntryConfiguration : IEntityTypeConfiguration<CoverageEntry>
{
    public void Configure(EntityTypeBuilder<CoverageEntry> builder)
    {
        builder.ToTable("CoverageEntries");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type)
            .IsRequired();

        builder.Property(c => c.PlanName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.AffiliateNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.HasIndex(c => new { c.PatientId, c.IsActive })
            .IsUnique()
            .HasFilter("[IsActive] = 1");
    }
}
