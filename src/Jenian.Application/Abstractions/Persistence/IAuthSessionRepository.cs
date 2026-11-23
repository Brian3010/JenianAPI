using Jenian.Application.Abstractions.Auth;
using Jenian.Application.Features.Auth.Dtos;

namespace Jenian.Application.Abstractions.Persistence
{
  /// <summary>
  /// Provides stored session and refresh-token history to the authentication lifecycle.
  /// </summary>
  /// <remarks>
  /// All writes require an active IAuthTransaction and the owning user's lock. They
  /// share its scoped auth context and never commit independently. Callers determine
  /// lifecycle eligibility from validated ownership, reloaded state, and database time.
  /// Write failures must propagate so the enclosing transaction can roll back.
  /// </remarks>
  public interface IAuthSessionRepository
  {
    /// <summary>
    /// Finds a refresh token by exact, case-sensitive value and matching session device ID.
    /// Returns null when the token is unknown or the device ID does not match.
    /// </summary>
    /// <remarks>
    /// Matches current and consumed tokens, including expired or revoked sessions.
    /// The lifecycle service determines eligibility from authoritative time and session state.
    /// The returned token must belong to the returned session.
    /// A lookup before locking identifies candidate ownership only. After acquiring user
    /// locks, callers must repeat the lookup, and implementations must reload authoritative
    /// state rather than return stale tracked values. Dependency failures must propagate
    /// rather than be converted to a missing credential. Credentials must never be logged.
    /// </remarks>
    Task<AuthCredentialSnapshot?> FindByCredentialsAsync(
      string refreshToken,
      Guid deviceId,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new session and its initial unconsumed refresh token together.
    /// </summary>
    /// <remarks>
    /// The initial token must belong to the supplied session and have no consumption
    /// time or duplicate result. The session must be unrevoked. Supplied IDs, device,
    /// creation time, issuance time, and deadline are persisted without recomputation.
    /// Existing sessions or tokens must not be overwritten.
    /// </remarks>
    Task CreateAsync(
      AuthSessionSnapshot session,
      AuthRefreshTokenSnapshot initialToken,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes the current token, stores its original result, inserts its successor,
    /// and updates the session deadline within the enclosing transaction.
    /// </summary>
    /// <remarks>
    /// The session must belong to userId, and currentTokenId must identify its unconsumed
    /// token. Reject inconsistent state instead of overwriting a prior rotation result.
    /// The successor must belong to the same session and be unconsumed with no duplicate
    /// result. Its value must match duplicateResult.RefreshToken, and the result's device
    /// must match the session. Persist consumedAtUtc and the complete duplicate result
    /// on the old token; use duplicateResult.SessionDeadlineUtc as the session deadline.
    /// Do not change the session's device or creation time, or any earlier token history.
    /// Persist consumption before inserting the successor to respect the unique current-token
    /// index. All steps must roll back together on failure. No commit occurs in this method.
    /// </remarks>
    Task RotateAsync(
      string userId,
      Guid sessionId,
      Guid currentTokenId,
      AuthRefreshTokenSnapshot successor,
      DateTimeOffset consumedAtUtc,
      AuthTokenResultDto duplicateResult,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Records revocation of one session belonging to the supplied user.
    /// </summary>
    /// <remarks>
    /// Missing sessions, ownership mismatches, and already revoked sessions are no-ops.
    /// Preserve existing revocation timestamps and reasons, session deadlines, and token history.
    /// Reuse rejection must commit this write before returning its credential failure.
    /// </remarks>
    Task RevokeSessionAsync(
      string userId,
      Guid sessionId,
      DateTimeOffset revokedAtUtc,
      string reason,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Records revocation of all unrevoked sessions belonging to the supplied user.
    /// </summary>
    /// <remarks>
    /// Preserve earlier revocations and all token history. Do not modify another user's
    /// sessions. Password reset and demo ending use this operation within the same auth
    /// transaction as the corresponding Identity changes.
    /// </remarks>
    Task RevokeUserSessionsAsync(
      string userId,
      DateTimeOffset revokedAtUtc,
      string reason,
      CancellationToken cancellationToken = default);
  }
}
