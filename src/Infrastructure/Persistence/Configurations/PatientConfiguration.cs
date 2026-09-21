using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.DocumentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.DocumentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(p => new { p.DocumentType, p.DocumentNumber })
            .IsUnique();
    }
}
