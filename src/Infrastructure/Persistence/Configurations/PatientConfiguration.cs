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

        builder.HasOne(p => p.ResponsiblePatient)
            .WithMany()
            .HasForeignKey(p => p.ResponsiblePatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.Contact, contact =>
        {
            contact.Property(c => c.MobilePhone)
                .IsRequired()
                .HasMaxLength(50);

            contact.Property(c => c.LandlinePhone)
                .HasMaxLength(50);

            contact.Property(c => c.Email)
                .HasMaxLength(200);
        });

        builder.OwnsOne(p => p.Address, address =>
        {
            address.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(200);

            address.Property(a => a.Number)
                .IsRequired()
                .HasMaxLength(50);

            address.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100);

            address.Property(a => a.Province)
                .IsRequired()
                .HasMaxLength(100);

            address.Property(a => a.PostalCode)
                .IsRequired()
                .HasMaxLength(20);
        });

        builder.HasMany(p => p.CoverageEntries)
            .WithOne(c => c.Patient)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
