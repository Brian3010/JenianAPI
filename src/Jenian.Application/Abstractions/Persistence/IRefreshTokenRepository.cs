namespace Jenian.Application.Abstractions.Persistence
{
  public interface IRefreshTokenRepository
  {
    Task RemoveByUserIdAsync(string userId, CancellationToken cancellationToken = default);
  }
}
