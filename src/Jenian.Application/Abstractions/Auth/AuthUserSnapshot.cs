namespace Jenian.Application.Abstractions.Auth
{
  /// <summary>
  /// User claims and demo state needed for authentication lifecycle decisions.
  /// </summary>
  public class AuthUserSnapshot
  {
    public required string Id { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required bool IsDemoUser { get; init; }
    public DateTimeOffset? DemoExpiresAtUtc { get; init; }
    public required bool IsDemoPendingDeletion { get; init; }
  }
}
