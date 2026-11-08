using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class AgendaConfiguration : IEntityTypeConfiguration<Agenda>
{
    public void Configure(EntityTypeBuilder<Agenda> builder)
    {
        builder.ToTable("Agendas");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ProfessionalId)
            .IsRequired();

        builder.HasIndex(a => a.ProfessionalId)
            .IsUnique();

        builder.Property(a => a.MaxSobreturnosPerDay)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasMany(a => a.Blocks)
            .WithOne()
            .HasForeignKey(b => b.AgendaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
