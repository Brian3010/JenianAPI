using Jenian.Application.Features.Auth.Dtos;

namespace Jenian.Application.Abstractions.Auth
{
  /// <summary>
  /// Stored refresh-token history used by the authentication lifecycle.
  /// </summary>
  /// <remarks>
  /// Current tokens have neither a consumption time nor a duplicate result.
  /// Consumed tokens retain both values, unchanged after successful rotation.
  /// A duplicate result belongs to this token's original rotation; later rotations
  /// do not replace it. Token validity is determined by the owning session, not
  /// a separate token expiry. Token values and duplicate credentials must never be logged.
  /// </remarks>
  public class AuthRefreshTokenSnapshot
  {
    public required Guid Id { get; init; }
    public required Guid SessionId { get; init; }
    public required string Token { get; init; }
    public required DateTimeOffset IssuedAtUtc { get; init; }
    public DateTimeOffset? ConsumedAtUtc { get; init; }
    public AuthTokenResultDto? DuplicateResult { get; init; }
  }
}
