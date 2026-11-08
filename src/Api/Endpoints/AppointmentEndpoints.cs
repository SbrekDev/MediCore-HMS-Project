using MediCore.Api.Authorization;
using MediCore.Application.Appointments;
using Microsoft.AspNetCore.Authorization;

namespace MediCore.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appointments");

        group.MapPost("/", [Authorize(Policy = AuthorizationPolicies.ReceptionStaff)] async (
            CreateAppointmentRequest request,
            BookAppointment.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new BookAppointment.Command(
                    request.PatientId,
                    request.ProfessionalId,
                    request.SpecialtyId,
                    request.Start,
                    request.IsSobreturno),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/appointments/{result.Value}", new { id = result.Value })
                : MapErrorResult(result.Error.Code, result.Error.Message);
        });

        return app;
    }

    private static IResult MapErrorResult(string code, string message)
    {
        return code switch
        {
            "Appointment.ProfessionalOverlap" => Results.Conflict(new { code, message }),
            "Appointment.PatientOverlap" => Results.Conflict(new { code, message }),
            "Appointment.SobreturnoNotAuthorized" => Results.Json(new { code, message }, statusCode: StatusCodes.Status403Forbidden),
            "Professional.NotFound" => Results.NotFound(new { code, message }),
            "Patient.NotFound" => Results.NotFound(new { code, message }),
            "Specialty.NotFound" => Results.NotFound(new { code, message }),
            _ => Results.BadRequest(new { code, message })
        };
    }
}

public sealed record CreateAppointmentRequest(
    Guid PatientId,
    Guid ProfessionalId,
    Guid SpecialtyId,
    DateTimeOffset Start,
    bool IsSobreturno = false);
