using Jenian.API.Contracts.Auth;
using Jenian.Application.Common;

namespace Jenian.Application.Abstractions.DemoAccount
{
  public interface IDemoAccountService
  {
    // create a demo session for a user with the given userId
    Task<ServiceResult<DemoLoginResult>> CreateDemoSessionAsync(string refreshToken, Guid deviceId, CancellationToken cancellationToken);


    // remove the demo session e.g. when user logs out or another on login is detected for the same userId, or when the demo session expires
    Task<ServiceResult<bool>> EndDemoSessionAsync(string userId, string refreshToken, Guid deviceId, CancellationToken cancellationToken);


    // delete expired demo accounts from the database, return the number of deleted accounts
    Task<int> DeleteExpiredDemoAccountAsync(CancellationToken cancellationToken);
  }


  public class DemoLoginResult
  {
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public required DateTimeOffset ExpiresAtUtc { get; init; }
    public required DateTimeOffset AccessTokenExpiresAtUtc { get; init; }
    public required UserDto User { get; init; }
  }
}
