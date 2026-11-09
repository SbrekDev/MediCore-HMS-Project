using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Appointments;

public static class ListAppointments
{
    public sealed record AppointmentListItem(
        Guid Id,
        Guid PatientId,
        Guid ProfessionalId,
        Guid SpecialtyId,
        DateTimeOffset Start,
        int DurationMinutes,
        string Status,
        bool IsSobreturno,
        bool IsLateCancellation);

    public sealed record Response(IReadOnlyList<AppointmentListItem> Appointments);

    public sealed record Query(
        DateTimeOffset From,
        DateTimeOffset To,
        Guid? ProfessionalId,
        Guid? PatientId,
        AppointmentStatus? Status);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<Response>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var queryable = _context.Appointments
                .AsNoTracking()
                .Where(a => a.StartDateTime >= query.From && a.StartDateTime < query.To);

            if (query.ProfessionalId.HasValue)
            {
                queryable = queryable.Where(a => a.ProfessionalId == query.ProfessionalId.Value);
            }

            if (query.PatientId.HasValue)
            {
                queryable = queryable.Where(a => a.PatientId == query.PatientId.Value);
            }

            if (query.Status.HasValue)
            {
                queryable = queryable.Where(a => a.Status == query.Status.Value);
            }

            var appointments = await queryable
                .OrderBy(a => a.StartDateTime)
                .Select(a => new AppointmentListItem(
                    a.Id,
                    a.PatientId,
                    a.ProfessionalId,
                    a.SpecialtyId,
                    a.StartDateTime,
                    a.DurationMinutes,
                    a.Status.ToString(),
                    a.IsSobreturno,
                    a.IsLateCancellation))
                .ToListAsync(cancellationToken);

            return Result<Response>.Success(new Response(appointments));
        }
    }
}
