namespace Jenian.Application.Abstractions.Auth
{
  /// <summary>
  /// Stored session state used by the authentication lifecycle.
  /// </summary>
  /// <remarks>
  /// Expiry and revocation eligibility are evaluated by the lifecycle service using
  /// authoritative time. The stored device ID and creation time do not change.
  /// </remarks>
  public class AuthSessionSnapshot
  {
    public required Guid Id { get; init; }
    public required string UserId { get; init; }
    public required Guid DeviceId { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset SessionDeadlineUtc { get; init; }
    public DateTimeOffset? RevokedAtUtc { get; init; }
    public string? RevocationReason { get; init; }
  }
}
