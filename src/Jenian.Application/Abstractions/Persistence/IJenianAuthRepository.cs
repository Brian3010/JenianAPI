namespace Jenian.Application.Abstractions.Persistence
{
  public interface IJenianAuthRepository
  {
    Task<bool> IsTelegramConnectedAsync(string userId);
  }
}
