using Jenian.Application.Common;

namespace Jenian.Application.Abstractions.Auth
{
  /// <summary>
  /// Coordinates lifecycle operations within the authentication database.
  /// </summary>
  public interface IAuthTransaction
  {
    /// <summary>
    /// Runs an operation in an auth-database transaction after acquiring exclusive,
    /// transaction-owned locks for the distinct supplied users in deterministic order.
    /// </summary>
    /// <remarks>
    /// The callback must reload and validate authoritative state after locks are acquired.
    /// Its disposition determines whether changes commit or roll back, independently
    /// of whether its service result represents success. In particular, reuse rejection
    /// must commit its session revocation before returning a credential failure.
    /// Exceptions and cancellation roll back the transaction; rollback must still be
    /// attempted when the operation's cancellation token has been canceled.
    /// A result is returned only after the requested commit or rollback completes.
    /// Transaction or lock failures must not be reported as a successful operation.
    /// Identity and session persistence must participate in the same auth transaction.
    /// </remarks>
    /// <param name="userIds">User IDs whose lifecycle changes require serialization.</param>
    /// <param name="operation">The operation to invoke after acquiring all user locks.</param>
    /// <param name="cancellationToken">Cancellation for lock acquisition and the operation.</param>
    /// <returns>The operation's service result after its transaction disposition completes.</returns>
    Task<ServiceResult<T>> ExecuteAsync<T>(
      IReadOnlyCollection<string> userIds,
      Func<CancellationToken, Task<AuthTransactionResult<T>>> operation,
      CancellationToken cancellationToken = default);
  }
}
