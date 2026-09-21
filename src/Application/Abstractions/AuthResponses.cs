namespace MediCore.Application.Abstractions;

public sealed record TokenResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);

public sealed record UserInfoResponse(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles);
