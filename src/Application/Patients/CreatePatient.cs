using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Patients;

public static class CreatePatient
{
    public sealed record ContactRequest(
        string MobilePhone,
        string? LandlinePhone,
        string? Email);

    public sealed record AddressRequest(
        string Street,
        string Number,
        string City,
        string Province,
        string PostalCode);

    public sealed record Command(
        string DocumentType,
        string DocumentNumber,
        string FirstName,
        string LastName,
        DateTimeOffset BirthDate,
        ContactRequest Contact,
        AddressRequest Address);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public Handler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            if (!IsValidDocumentType(command.DocumentType))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Patient.InvalidDocumentType",
                    $"Document type '{command.DocumentType}' is not supported."));
            }

            if (string.IsNullOrWhiteSpace(command.DocumentNumber))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Patient.DocumentNumberRequired",
                    "Document number is required."));
            }

            if (string.IsNullOrWhiteSpace(command.FirstName))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Patient.FirstNameRequired",
                    "First name is required."));
            }

            if (string.IsNullOrWhiteSpace(command.LastName))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Patient.LastNameRequired",
                    "Last name is required."));
            }

            if (string.IsNullOrWhiteSpace(command.Contact.MobilePhone))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Patient.MobilePhoneRequired",
                    "Mobile phone is required."));
            }

            if (string.IsNullOrWhiteSpace(command.Address.Street)
                || string.IsNullOrWhiteSpace(command.Address.Number)
                || string.IsNullOrWhiteSpace(command.Address.City)
                || string.IsNullOrWhiteSpace(command.Address.Province)
                || string.IsNullOrWhiteSpace(command.Address.PostalCode))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Patient.AddressIncomplete",
                    "Street, number, city, province and postal code are required."));
            }

            var existing = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.DocumentType == command.DocumentType && p.DocumentNumber == command.DocumentNumber,
                    cancellationToken);

            if (existing is not null)
            {
                return Result<Guid>.Failure(DomainError.Conflict(
                    "Patient.DuplicateDocument",
                    $"A patient with document {command.DocumentType} {command.DocumentNumber} already exists."));
            }

            var now = DateTimeOffset.UtcNow;
            var userId = _currentUserService.UserId ?? Guid.Empty;

            var patient = new Patient(Guid.NewGuid())
            {
                DocumentType = command.DocumentType,
                DocumentNumber = command.DocumentNumber,
                FirstName = command.FirstName,
                LastName = command.LastName,
                BirthDate = command.BirthDate,
                Contact = new Contact
                {
                    MobilePhone = command.Contact.MobilePhone,
                    LandlinePhone = command.Contact.LandlinePhone,
                    Email = command.Contact.Email
                },
                Address = new Address
                {
                    Street = command.Address.Street,
                    Number = command.Address.Number,
                    City = command.Address.City,
                    Province = command.Address.Province,
                    PostalCode = command.Address.PostalCode
                },
                CreatedAt = now,
                CreatedBy = userId,
                ModifiedAt = now,
                ModifiedBy = userId
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(patient.Id);
        }

        private static bool IsValidDocumentType(string documentType)
        {
            return documentType switch
            {
                "DNI" or "Pasaporte" or "DNIExtranjero" or "LC" or "LE" => true,
                _ => false
            };
        }
    }
}
