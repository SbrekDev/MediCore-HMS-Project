using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class AgendaExceptionConfiguration : IEntityTypeConfiguration<AgendaException>
{
    public void Configure(EntityTypeBuilder<AgendaException> builder)
    {
        builder.ToTable("AgendaExceptions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AgendaId)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(e => new { e.AgendaId, e.Date })
            .IsUnique();

        builder.HasOne<Agenda>()
            .WithMany()
            .HasForeignKey(e => e.AgendaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
