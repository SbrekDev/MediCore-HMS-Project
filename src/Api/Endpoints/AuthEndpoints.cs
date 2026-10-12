using MediCore.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace MediCore.Api.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "refreshToken";
    private const string RefreshCookiePath = "/api/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (
            LoginRequest request,
            IJwtTokenService tokenService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await tokenService.CreateTokenPairAsync(
                request.Email,
                request.Password,
                cancellationToken);

            if (result is null)
            {
                return Results.Unauthorized();
            }

            AppendRefreshTokenCookie(httpContext, result.RefreshToken);

            return Results.Ok(new
            {
                result.UserId,
                result.Email,
                result.AccessToken,
                result.ExpiresAt
            });
        });

        group.MapPost("/refresh", async (
            IJwtTokenService tokenService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Results.Unauthorized();
            }

            var result = await tokenService.RefreshTokenPairAsync(refreshToken, cancellationToken);
            if (result is null)
            {
                httpContext.Response.Cookies.Delete(RefreshTokenCookieName);
                return Results.Unauthorized();
            }

            AppendRefreshTokenCookie(httpContext, result.RefreshToken);

            return Results.Ok(new
            {
                result.UserId,
                result.Email,
                result.AccessToken,
                result.ExpiresAt
            });
        });

        group.MapPost("/logout", [Authorize] async (
            IJwtTokenService tokenService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await tokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
            }

            httpContext.Response.Cookies.Delete(RefreshTokenCookieName);
            return Results.NoContent();
        });

        group.MapGet("/me", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] () =>
        {
            return Results.Ok(new { message = "Authenticated" });
        });

        return app;
    }

    private static void AppendRefreshTokenCookie(HttpContext httpContext, string refreshToken)
    {
        httpContext.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

}

public sealed record LoginRequest(string Email, string Password);
