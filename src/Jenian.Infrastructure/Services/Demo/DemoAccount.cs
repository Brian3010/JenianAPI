using Jenian.API.Contracts.Auth;
using Jenian.Application.Abstractions.Auth;
using Jenian.Application.Abstractions.DemoAccount;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common;
using Jenian.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.Services.Demo
{
  public class DemoAccount : IDemoAccountService
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenManager _jwtTokenManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly ILogger<DemoAccount> _logger;

    public DemoAccount(
      UserManager<ApplicationUser> userManager,
      IJwtTokenManager jwtTokenManager,
      ILogger<DemoAccount> logger,
      IRefreshTokenRepository refreshTokenRepository,
      IShiftRepository shiftRepository

      ) {
      _userManager = userManager;
      _jwtTokenManager = jwtTokenManager;
      _logger = logger;
      _refreshTokenRepository = refreshTokenRepository;
      _shiftRepository = shiftRepository;
    }

    public async Task<ServiceResult<DemoLoginResult>> CreateDemoSessionAsync(string refreshToken, Guid deviceId, CancellationToken cancellationToken) {

      // delete old expired demo accounts 
      await DeleteExpiredDemoAccountAsync(cancellationToken);

      // create internal demo user
      var user = new ApplicationUser {
        UserName = $"demo_{Guid.NewGuid().ToString("N")}",
        Email = $"demo_{Guid.NewGuid().ToString("N")}@example.com",
        EmailConfirmed = true,
        IsDemoUser = true,
        DemoStatus = DemoAccountStatus.Active,
        DemoCreatedAtUtc = DateTimeOffset.UtcNow,
        DemoExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
      };

      var identityResult = await _userManager.CreateAsync(user);
      if (!identityResult.Succeeded) {
        return ServiceResult<DemoLoginResult>.Failure(["Failed to create demo account."]);
      }

      // Generate JWT access token with a shorter expiration than the demo account's expiration
      var normalExpiration = DateTimeOffset.UtcNow.AddMinutes(30);
      var accessTokenExpiration = new[] { normalExpiration, user.DemoExpiresAtUtc.Value }.Min();
      var demoAccessToken = _jwtTokenManager.GenerateJwtToken(
        new JwtUserClaims(user.Id, user.UserName, user.Email, user.IsDemoUser),
        accessTokenExpiration);
      await _jwtTokenManager.UpsertDeviceAuthInfoAsync(refreshToken, deviceId, user.Id);


      var demoLoginResult = new DemoLoginResult {
        AccessToken = demoAccessToken,
        RefreshToken = refreshToken,
        ExpiresAtUtc = user.DemoExpiresAtUtc.Value,
        AccessTokenExpiresAtUtc = accessTokenExpiration,
        User = new UserDto {
          Id = user.Id,
          UserName = user.UserName,
          Email = user.Email,
          IsTelegramConnected = false
        }
      };
      return ServiceResult<DemoLoginResult>.Success(demoLoginResult);
    }

    // Deletes expired demo accounts and their associated data from the database.
    // triggered when user calls demo login
    public async Task<int> DeleteExpiredDemoAccountAsync(CancellationToken cancellationToken) {
      // Accounts that expired less than 5 minutes ago are skipped to allow 
      // any in-flight database transactions to finish cleanly before row deletion.
      var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-5);

      // Fetch expired or pending-deletion demo users
      var expiredUsers = await _userManager.Users
          .Where(u => u.IsDemoUser &&
                     (u.DemoStatus == DemoAccountStatus.PendingDeletion ||
                      u.DemoExpiresAtUtc <= cutoffTime)).Take(50)
          .ToListAsync(cancellationToken);


      _logger.LogInformation("Found {Count} expired demo accounts to clean up.", expiredUsers.Count);

      foreach (var user in expiredUsers) {
        try {
          // Cascading cleanup of dependent domain records 
          await _refreshTokenRepository.RemoveByUserIdAsync(user.Id, cancellationToken);
          await _shiftRepository.RemoveShiftsByUserIdAsync(user.Id, cancellationToken);
          await _shiftRepository.RemovePayCycleSettingsByUserIdAsync(user.Id, cancellationToken);

          await _userManager.DeleteAsync(user);
        } catch (Exception ex) {
          // Log & continue so one failed deletion doesn't block the rest
          _logger.LogError(ex, "Failed to delete expired demo user {UserId}", user.Id);
        }
      }

      return expiredUsers.Count;
    }

    public async Task<ServiceResult<bool>> EndDemoSessionAsync(string userId, string refreshToken, Guid deviceId, CancellationToken cancellationToken) {
      // Fetch the user
      var user = await _userManager.FindByIdAsync(userId);

      // If user doesn't exist or isn't a demo user, return success gracefully
      if (user is null || !user.IsDemoUser) {
        return ServiceResult<bool>.Success(true);
      }

      //Transition account status to PendingDeletion & force expiry to now
      user.DemoStatus = DemoAccountStatus.PendingDeletion;
      user.DemoExpiresAtUtc = DateTimeOffset.UtcNow;

      var updateResult = await _userManager.UpdateAsync(user);
      if (!updateResult.Succeeded) {
        _logger.LogWarning("Failed to update demo status to PendingDeletion for user {UserId}", userId);
        return ServiceResult<bool>.Failure(["Failed to end demo session."]);
      }

      // Immediately revoke active refresh tokens so the user cannot issue new JWT access tokens
      await _jwtTokenManager.RevokeDeviceAuthInfoAsync(refreshToken, deviceId, userId);

      _logger.LogInformation("Demo session ended for user {UserId}. Flagged as PendingDeletion.", userId);

      return ServiceResult<bool>.Success(true);
    }
  }
}
