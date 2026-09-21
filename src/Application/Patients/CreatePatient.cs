using MediCore.Application.Abstractions;
using MediCore.Domain.Entities;

namespace MediCore.Application.Patients;

public static class CreatePatient
{
    public sealed record Command(
        string DocumentType,
        string DocumentNumber,
        string FirstName,
        string LastName,
        DateTimeOffset BirthDate);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public Handler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var patient = new Patient(Guid.NewGuid())
            {
                DocumentType = command.DocumentType,
                DocumentNumber = command.DocumentNumber,
                FirstName = command.FirstName,
                LastName = command.LastName,
                BirthDate = command.BirthDate,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = _currentUserService.UserId ?? Guid.Empty,
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedBy = _currentUserService.UserId ?? Guid.Empty
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync(cancellationToken);

            return patient.Id;
        }
    }
}
