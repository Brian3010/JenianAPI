namespace Jenian.Application.Abstractions.Auth
{
  /// <summary>
  /// Minimal user claims needed to generate a JWT access token.
  /// Keeps the token manager decoupled from the Identity <c>ApplicationUser</c>.
  /// </summary>
  public record JwtUserClaims(string Id, string UserName, string Email, bool IsDemoUser);

  public interface IJwtTokenManager
  {
    string GenerateJwtToken(JwtUserClaims user, int TTLInMinute = 5); // short-lived access token

    string GenerateJwtToken(JwtUserClaims user, DateTimeOffset expiresAtUtc);

    string GenerateRefreshToken();

    Task UpdateDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId);

    Task StoreDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId);

    Task<bool> DeviceAuthInfoExistsAsync(string refreshToken, Guid deviceId, string userId);

    Task<bool> IsRefreshTokenExpiredAsync(string refreshToken);

    Task UpsertDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId);

    Task RevokeDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId);

    Task<string?> GetUserIdByDeviceAuthAsync(string refreshToken, Guid deviceId);
  }
}
