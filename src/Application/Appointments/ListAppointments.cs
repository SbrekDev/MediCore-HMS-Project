using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Enums;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Appointments;

public static class ListAppointments
{
    public sealed record AppointmentListItem(
        Guid Id,
        Guid PatientId,
        string PatientName,
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
        private readonly ICurrentUserService _currentUserService;

        public Handler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Response>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var effectiveProfessionalId = query.ProfessionalId;

            if (_currentUserService.Roles.Contains("Professional"))
            {
                var myProfessionalId = _currentUserService.ProfessionalId;
                if (myProfessionalId is null)
                {
                    return Result<Response>.Failure(DomainError.Forbidden(
                        "Appointment.NotOwner",
                        "This account is not linked to a professional."));
                }

                if (effectiveProfessionalId.HasValue && effectiveProfessionalId.Value != myProfessionalId.Value)
                {
                    return Result<Response>.Failure(DomainError.Forbidden(
                        "Appointment.NotOwner",
                        "You can only view your own appointments."));
                }

                effectiveProfessionalId = myProfessionalId;
            }

            var queryable = _context.Appointments
                .AsNoTracking()
                .Where(a => a.StartDateTime >= query.From && a.StartDateTime < query.To);

            if (effectiveProfessionalId.HasValue)
            {
                queryable = queryable.Where(a => a.ProfessionalId == effectiveProfessionalId.Value);
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
                .Join(
                    _context.Patients,
                    a => a.PatientId,
                    p => p.Id,
                    (a, p) => new { a, p })
                .OrderBy(x => x.a.StartDateTime)
                .Select(x => new AppointmentListItem(
                    x.a.Id,
                    x.a.PatientId,
                    $"{x.p.FirstName} {x.p.LastName}",
                    x.a.ProfessionalId,
                    x.a.SpecialtyId,
                    x.a.StartDateTime,
                    x.a.DurationMinutes,
                    x.a.Status.ToString(),
                    x.a.IsSobreturno,
                    x.a.IsLateCancellation))
                .ToListAsync(cancellationToken);

            return Result<Response>.Success(new Response(appointments));
        }
    }
}
