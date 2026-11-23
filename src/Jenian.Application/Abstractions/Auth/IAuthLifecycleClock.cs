namespace Jenian.Application.Abstractions.Auth
{
  /// <summary>
  /// Supplies authoritative database time for authentication lifecycle decisions.
  /// </summary>
  public interface IAuthLifecycleClock
  {
    /// <summary>
    /// Reads the current UTC time from SQL Server within the active auth transaction.
    /// </summary>
    /// <remarks>
    /// Call only after acquiring the operation's user locks, so time spent waiting
    /// for locks does not shorten session lifetimes or the rotation grace period.
    /// Sample rotation time with the final state transition and commit immediately.
    /// Database failures must propagate without falling back to the API server's clock.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation for the database time read.</param>
    /// <returns>The database's current time with a zero UTC offset.</returns>
    Task<DateTimeOffset> GetUtcNowAsync(CancellationToken cancellationToken = default);
  }
}
