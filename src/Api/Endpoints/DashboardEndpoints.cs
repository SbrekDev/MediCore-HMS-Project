using MediCore.Api.Authorization;
using MediCore.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCore.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard/summary", [Authorize(Policy = AuthorizationPolicies.ClinicalStaff)] async (
            [FromQuery] string? from,
            [FromQuery] string? to,
            GetDashboardSummary.Handler handler,
            CancellationToken cancellationToken) =>
        {
            if (!TryParseInstant(from, out var rangeStart) || !TryParseInstant(to, out var rangeEnd))
            {
                return Results.BadRequest(new
                {
                    code = "Dashboard.RangeRequired",
                    message = "A from and to instant must be provided, each with an explicit offset."
                });
            }

            if (rangeEnd <= rangeStart)
            {
                return Results.BadRequest(new
                {
                    code = "Dashboard.InvalidRange",
                    message = "The to instant must be after the from instant."
                });
            }

            var result = await handler.HandleAsync(
                new GetDashboardSummary.Query(rangeStart, rangeEnd),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
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
            _ => Results.BadRequest(new { code, message })
        };
    }
}
