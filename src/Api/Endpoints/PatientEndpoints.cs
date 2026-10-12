using MediCore.Api.Authorization;
using MediCore.Application.Patients;
using Microsoft.AspNetCore.Authorization;

namespace MediCore.Api.Endpoints;

public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patients");

        group.MapPost("/", [Authorize(Policy = AuthorizationPolicies.ReceptionStaff)] async (
            CreatePatient.Command command,
            CreatePatient.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/patients/{result.Value}", new { patientId = result.Value })
                : MapErrorResult(result.Error.Code, result.Error.Message);
        });

        group.MapGet("/", [Authorize(Policy = AuthorizationPolicies.ClinicalStaff)] async (
            ListPatients.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var patients = await handler.HandleAsync(cancellationToken);
            return Results.Ok(patients);
        });

        group.MapGet("/{id:guid}", [Authorize(Policy = AuthorizationPolicies.ClinicalStaff)] async (
            Guid id,
            GetPatient.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapErrorResult(result.Error.Code, result.Error.Message);
        });

        group.MapPut("/{id:guid}", [Authorize(Policy = AuthorizationPolicies.ReceptionStaff)] async (
            Guid id,
            UpdatePatientRequest request,
            UpdatePatient.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new UpdatePatient.Command(
                    id,
                    request.FirstName,
                    request.LastName,
                    request.BirthDate,
                    new CreatePatient.ContactRequest(
                        request.Contact.MobilePhone,
                        request.Contact.LandlinePhone,
                        request.Contact.Email),
                    new CreatePatient.AddressRequest(
                        request.Address.Street,
                        request.Address.Number,
                        request.Address.City,
                        request.Address.Province,
                        request.Address.PostalCode)),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : MapErrorResult(result.Error.Code, result.Error.Message);
        });

        return app;
    }

    private static IResult MapErrorResult(string code, string message)
    {
        return code switch
        {
            "Patient.NotFound" => Results.NotFound(new { code, message }),
            "Patient.DuplicateDocument" => Results.Conflict(new { code, message }),
            _ => Results.BadRequest(new { code, message })
        };
    }
}

public sealed record UpdatePatientContactRequest(
    string MobilePhone,
    string? LandlinePhone,
    string? Email);

public sealed record UpdatePatientAddressRequest(
    string Street,
    string Number,
    string City,
    string Province,
    string PostalCode);

public sealed record UpdatePatientRequest(
    string FirstName,
    string LastName,
    DateTimeOffset BirthDate,
    UpdatePatientContactRequest Contact,
    UpdatePatientAddressRequest Address);
