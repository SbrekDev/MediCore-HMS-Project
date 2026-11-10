using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Dashboard;

public static class GetDashboardSummary
{
    public sealed record AppointmentCounts(
        int Total,
        int Confirmed,
        int Completed,
        int NoShow,
        int Cancelled,
        int Sobreturnos);

    public sealed record PatientCounts(int Total, int AddedInRange);

    public sealed record ProfessionalCounts(int Total, int WithTemplate);

    public sealed record Response(
        DateTimeOffset From,
        DateTimeOffset To,
        AppointmentCounts Appointments,
        PatientCounts Patients,
        ProfessionalCounts Professionals);

    public sealed record Query(DateTimeOffset From, DateTimeOffset To);

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
            var appointments = _context.Appointments
                .AsNoTracking()
                .Where(a => a.StartDateTime >= query.From && a.StartDateTime < query.To);

            var total = await appointments.CountAsync(cancellationToken);
            var confirmed = await appointments.CountAsync(a => a.Status == AppointmentStatus.Confirmed, cancellationToken);
            var completed = await appointments.CountAsync(a => a.Status == AppointmentStatus.Completed, cancellationToken);
            var noShow = await appointments.CountAsync(a => a.Status == AppointmentStatus.NoShow, cancellationToken);
            var cancelled = await appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled, cancellationToken);
            var sobreturnos = await appointments.CountAsync(a => a.IsSobreturno, cancellationToken);

            var patientsTotal = await _context.Patients
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var patientsAdded = await _context.Patients
                .AsNoTracking()
                .CountAsync(p => p.CreatedAt >= query.From && p.CreatedAt < query.To, cancellationToken);

            var professionalsTotal = await _context.Professionals
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var professionalsWithTemplate = await _context.Agendas
                .AsNoTracking()
                .Where(a => a.Blocks.Any())
                .Select(a => a.ProfessionalId)
                .Distinct()
                .CountAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                query.From,
                query.To,
                new AppointmentCounts(total, confirmed, completed, noShow, cancelled, sobreturnos),
                new PatientCounts(patientsTotal, patientsAdded),
                new ProfessionalCounts(professionalsTotal, professionalsWithTemplate)));
        }
    }
}
