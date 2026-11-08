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

        group.MapPost("/{id:guid}/reschedule", [Authorize(Policy = AuthorizationPolicies.ReceptionStaff)] async (
            Guid id,
            RescheduleAppointmentRequest request,
            RescheduleAppointment.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new RescheduleAppointment.Command(id, request.Start),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { id = result.Value, start = request.Start.ToUniversalTime() })
                : MapErrorResult(result.Error.Code, result.Error.Message);
        });

        group.MapPost("/{id:guid}/cancel", [Authorize(Policy = AuthorizationPolicies.ReceptionStaff)] async (
            Guid id,
            CancelAppointment.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new CancelAppointment.Command(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { id = result.Value, status = "Cancelled" })
                : MapErrorResult(result.Error.Code, result.Error.Message);
        });

        group.MapPost("/{id:guid}/complete", [Authorize(Policy = AuthorizationPolicies.ClinicalStaff)] async (
            Guid id,
            CompleteAppointment.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new CompleteAppointment.Command(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { id = result.Value, status = "Completed" })
                : MapErrorResult(result.Error.Code, result.Error.Message);
        });

        group.MapPost("/{id:guid}/no-show", [Authorize(Policy = AuthorizationPolicies.ClinicalStaff)] async (
            Guid id,
            MarkNoShow.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new MarkNoShow.Command(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { id = result.Value, status = "NoShow" })
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
            "Appointment.InvalidState" => Results.Conflict(new { code, message }),
            "Appointment.NotFound" => Results.NotFound(new { code, message }),
            "Appointment.SobreturnoNotAuthorized" => Results.Json(new { code, message }, statusCode: StatusCodes.Status403Forbidden),
            "Appointment.NotOwner" => Results.Json(new { code, message }, statusCode: StatusCodes.Status403Forbidden),
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

public sealed record RescheduleAppointmentRequest(DateTimeOffset Start);
