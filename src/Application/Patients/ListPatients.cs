using MediCore.Application.Abstractions;
using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Patients;

public static class ListPatients
{
    public sealed record ContactDto(
        string MobilePhone,
        string? LandlinePhone,
        string? Email);

    public sealed record AddressDto(
        string Street,
        string Number,
        string City,
        string Province,
        string PostalCode);

    public sealed record PatientDto(
        Guid Id,
        string DocumentType,
        string DocumentNumber,
        string FirstName,
        string LastName,
        DateTimeOffset BirthDate,
        ContactDto Contact,
        AddressDto Address,
        bool ConsentPending);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PatientDto>> HandleAsync(
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Patients
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    p.DocumentNumber.Contains(search) ||
                    p.FirstName.ToLower().Contains(normalizedSearch) ||
                    p.LastName.ToLower().Contains(normalizedSearch));
            }

            var patients = await query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync(cancellationToken);

            return patients.Select(MapToDto).ToList();
        }
    }

    public static PatientDto MapToDto(Patient patient)
    {
        return new PatientDto(
            patient.Id,
            patient.DocumentType,
            patient.DocumentNumber,
            patient.FirstName,
            patient.LastName,
            patient.BirthDate,
            new ContactDto(
                patient.Contact.MobilePhone,
                patient.Contact.LandlinePhone,
                patient.Contact.Email),
            new AddressDto(
                patient.Address.Street,
                patient.Address.Number,
                patient.Address.City,
                patient.Address.Province,
                patient.Address.PostalCode),
            patient.ConsentPending);
    }
}
