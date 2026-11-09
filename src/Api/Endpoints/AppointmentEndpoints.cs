using MediCore.Api.Authorization;
using MediCore.Application.Appointments;
using MediCore.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        group.MapGet("/", [Authorize(Policy = AuthorizationPolicies.ClinicalStaff)] async (
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] Guid? professionalId,
            [FromQuery] Guid? patientId,
            [FromQuery] string? status,
            ListAppointments.Handler handler,
            CancellationToken cancellationToken) =>
        {
            if (!TryParseInstant(from, out var rangeStart) || !TryParseInstant(to, out var rangeEnd))
            {
                return Results.BadRequest(new
                {
                    code = "Appointment.RangeRequired",
                    message = "A from and to instant must be provided, each with an explicit offset."
                });
            }

            if (rangeEnd <= rangeStart)
            {
                return Results.BadRequest(new
                {
                    code = "Appointment.InvalidRange",
                    message = "The to instant must be after the from instant."
                });
            }

            AppointmentStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<AppointmentStatus>(status, true, out var parsedStatusValue))
                {
                    return Results.BadRequest(new
                    {
                        code = "Appointment.InvalidStatus",
                        message = $"Status '{status}' is not valid."
                    });
                }

                parsedStatus = parsedStatusValue;
            }

            var result = await handler.HandleAsync(
                new ListAppointments.Query(rangeStart, rangeEnd, professionalId, patientId, parsedStatus),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value.Appointments)
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
                ? Results.Ok(new
                {
                    id = result.Value.AppointmentId,
                    status = "Cancelled",
                    isLateCancellation = result.Value.IsLateCancellation
                })
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

    private static readonly string[] InstantFormats =
    [
        "yyyy-MM-ddTHH:mm:ssK",
        "yyyy-MM-ddTHH:mmK",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK"
    ];

    /// <summary>
    /// A day is a range of instants, so the range comes from the client with explicit offsets
    /// instead of being inferred from a bare date here (the server must not guess a time zone).
    /// </summary>
    private static bool TryParseInstant(string? value, out DateTimeOffset result)
    {
        result = default;

        return !string.IsNullOrWhiteSpace(value) &&
               HasExplicitOffset(value) &&
               DateTimeOffset.TryParseExact(
                   value,
                   InstantFormats,
                   System.Globalization.CultureInfo.InvariantCulture,
                   System.Globalization.DateTimeStyles.None,
                   out result);
    }

    /// <summary>
    /// <c>K</c> matches an empty offset, and <see cref="DateTimeOffset.TryParseExact(string?, string[]?, IFormatProvider?, DateTimeStyles, out DateTimeOffset)"/>
    /// then fills it in from the server's zone. That is the same guess refused above, one layer
    /// down, so the offset has to be present in the text itself.
    /// </summary>
    private static bool HasExplicitOffset(string value)
    {
        var timeStart = value.IndexOf('T', StringComparison.Ordinal);

        if (timeStart < 0 || timeStart + 1 >= value.Length)
        {
            return false;
        }

        var time = value.AsSpan(timeStart + 1);

        // Neither '+' nor '-' can appear in the time-of-day part, so either one there is the
        // offset. A trailing 'Z' is the UTC designator form of the same thing.
        return time[^1] is 'Z' or 'z' || time.Contains('+') || time.Contains('-');
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
