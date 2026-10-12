using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Patients;

public static class UpdatePatient
{
    public sealed record Command(
        Guid PatientId,
        string FirstName,
        string LastName,
        DateTimeOffset BirthDate,
        CreatePatient.ContactRequest Contact,
        CreatePatient.AddressRequest Address);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public Handler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Unit>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.FirstName))
            {
                return Result<Unit>.Failure(DomainError.Validation(
                    "Patient.FirstNameRequired",
                    "First name is required."));
            }

            if (string.IsNullOrWhiteSpace(command.LastName))
            {
                return Result<Unit>.Failure(DomainError.Validation(
                    "Patient.LastNameRequired",
                    "Last name is required."));
            }

            if (string.IsNullOrWhiteSpace(command.Contact.MobilePhone))
            {
                return Result<Unit>.Failure(DomainError.Validation(
                    "Patient.MobilePhoneRequired",
                    "Mobile phone is required."));
            }

            if (string.IsNullOrWhiteSpace(command.Address.Street)
                || string.IsNullOrWhiteSpace(command.Address.Number)
                || string.IsNullOrWhiteSpace(command.Address.City)
                || string.IsNullOrWhiteSpace(command.Address.Province)
                || string.IsNullOrWhiteSpace(command.Address.PostalCode))
            {
                return Result<Unit>.Failure(DomainError.Validation(
                    "Patient.AddressIncomplete",
                    "Street, number, city, province and postal code are required."));
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);

            if (patient is null)
            {
                return Result<Unit>.Failure(DomainError.NotFound(
                    "Patient.NotFound",
                    $"Patient '{command.PatientId}' was not found."));
            }

            patient.FirstName = command.FirstName;
            patient.LastName = command.LastName;
            patient.BirthDate = command.BirthDate;
            patient.Contact = new Contact
            {
                MobilePhone = command.Contact.MobilePhone,
                LandlinePhone = command.Contact.LandlinePhone,
                Email = command.Contact.Email
            };
            patient.Address = new Address
            {
                Street = command.Address.Street,
                Number = command.Address.Number,
                City = command.Address.City,
                Province = command.Address.Province,
                PostalCode = command.Address.PostalCode
            };
            patient.ModifiedAt = DateTimeOffset.UtcNow;
            patient.ModifiedBy = _currentUserService.UserId ?? Guid.Empty;

            await _context.SaveChangesAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
    }
}
