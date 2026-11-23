using Jenian.Application.Common;

namespace Jenian.Application.Abstractions.Auth
{
  /// <summary>
  /// Provides Identity operations required by the authentication lifecycle.
  /// </summary>
  /// <remarks>
  /// Implementations share the scoped auth context used by session persistence and
  /// IAuthTransaction. Mutations participate in the caller's transaction and never
  /// commit it independently. Failed tracked changes must not leak into later operations
  /// after rollback. Credentials and reset tokens must never be logged.
  /// Dependency failures must remain distinguishable from rejected credentials or
  /// validation failures; lookup failures must not be converted to a missing user.
  /// </remarks>
  public interface IAuthIdentityGateway
  {
    /// <summary>
    /// Finds a user by email first, then by user name, preserving existing login lookup order.
    /// Returns null when neither lookup finds a user.
    /// </summary>
    /// <remarks>
    /// A lookup before locking identifies a candidate only. Reload by ID and revalidate
    /// the login identifier after acquiring its user lock before checking the password.
    /// </remarks>
    Task<AuthUserSnapshot?> FindByLoginAsync(
      string login,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by email, returning null when no user exists.
    /// </summary>
    /// <remarks>
    /// Password reset must reload and revalidate the candidate after acquiring its user lock.
    /// </remarks>
    Task<AuthUserSnapshot?> FindByEmailAsync(
      string email,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads current user claims and demo state by ID, returning null when no user exists.
    /// </summary>
    /// <remarks>
    /// Reads after locking must reload authoritative state rather than return stale tracked values.
    /// </remarks>
    Task<AuthUserSnapshot?> FindByIdAsync(
      string userId,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the current password under the user's lock and active auth transaction.
    /// Returns false for a missing user or incorrect password.
    /// </summary>
    Task<bool> CheckPasswordAsync(
      string userId,
      string password,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a decoded Identity reset token and changes the user's password.
    /// </summary>
    /// <remarks>
    /// Requires the user's lock and active auth transaction. Success means the password
    /// change is staged or saved within that transaction, not committed. The caller must
    /// revoke all existing refresh sessions before committing; any failure rolls back both.
    /// </remarks>
    Task<ServiceResult<bool>> ResetPasswordAsync(
      string userId,
      string resetToken,
      string newPassword,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an active demo user with the supplied ID and fixed UTC creation and expiry times.
    /// </summary>
    /// <remarks>
    /// The caller allocates the user ID before locking it, then supplies lifecycle-clock
    /// timestamps from within the auth transaction. The gateway creates the internal demo
    /// name and email. User creation and session creation must commit or roll back together.
    /// </remarks>
    Task<ServiceResult<AuthUserSnapshot>> CreateDemoUserAsync(
      string userId,
      DateTimeOffset createdAtUtc,
      DateTimeOffset expiresAtUtc,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a demo user pending deletion and forces expiry to the supplied UTC time.
    /// Missing users and regular users are successful no-ops.
    /// </summary>
    /// <remarks>
    /// Requires the user's lock and active auth transaction. The caller revokes the demo
    /// user's refresh sessions in the same transaction. This method does not delete domain data.
    /// </remarks>
    Task<ServiceResult<bool>> EndDemoUserAsync(
      string userId,
      DateTimeOffset endedAtUtc,
      CancellationToken cancellationToken = default);
  }
}
