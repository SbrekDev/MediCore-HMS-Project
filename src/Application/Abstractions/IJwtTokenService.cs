namespace MediCore.Application.Abstractions;

public interface IJwtTokenService
{
    Task<TokenResponse?> CreateTokenPairAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<TokenResponse?> RefreshTokenPairAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<UserInfoResponse?> GetUserInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
