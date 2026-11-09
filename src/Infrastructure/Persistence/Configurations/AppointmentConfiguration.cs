using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PatientId)
            .IsRequired();

        builder.Property(a => a.ProfessionalId)
            .IsRequired();

        builder.Property(a => a.SpecialtyId)
            .IsRequired();

        builder.Property(a => a.StartDateTime)
            .IsRequired();

        builder.Property(a => a.DurationMinutes)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired();

        builder.Property(a => a.IsSobreturno)
            .IsRequired();

        builder.Property(a => a.IsLateCancellation)
            .IsRequired();

        builder.HasIndex(a => new { a.ProfessionalId, a.StartDateTime });

        builder.HasIndex(a => new { a.PatientId, a.StartDateTime });

        // Referential integrity that until now only the application enforced: an appointment
        // must point at rows that exist, and neither the patient nor the professional can be
        // deleted while an appointment still uses it.
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Professional>()
            .WithMany()
            .HasForeignKey(a => a.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.RowVersion)
            .IsRowVersion();
    }
}
