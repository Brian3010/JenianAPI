using Jenian.API.Contracts.Auth;
using Jenian.Application.Abstractions.Auth;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common;
using Jenian.Application.Features.Auth.Commands;
using Jenian.Application.Features.Auth.Dtos;
using Jenian.Infrastructure.Identity.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Jenian.Infrastructure.Identity
{
  public class AuthService : IAuthService
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenManager _jwtTokenManager;
    private readonly IJenianAuthRepository _jenainAuthRepository;
    private readonly ILogger<AuthService> _logger;
    private readonly RegistrationOptions _registrationOptions;

    public AuthService(UserManager<ApplicationUser> userManager,
      IJwtTokenManager jwtTokenManager,
      IJenianAuthRepository jenainAuthRepository,
      ILogger<AuthService> logger,
      IOptions<RegistrationOptions> registrationOptions

      ) {
      _userManager = userManager;
      _jwtTokenManager = jwtTokenManager;
      _jenainAuthRepository = jenainAuthRepository;
      _logger = logger;
      _registrationOptions = registrationOptions.Value;
    }

    public async Task<ServiceResult<bool>> HasTelegramConnectedAsync(string userId, CancellationToken cancellationToken) {
      try {
        var isConnected = await _jenainAuthRepository.IsTelegramConnectedAsync(userId);
        return ServiceResult<bool>.Success(isConnected);
      } catch {
        return ServiceResult<bool>.Failure(["Couldn't determine Telegram connection status."]);
      }

    }

    public async Task<ServiceResult<AuthResultDto>> LoginAsync(LoginCommand command, CancellationToken cancellationToken) {
      var user = await _userManager.FindByEmailAsync(command.UserName) ?? await _userManager.FindByNameAsync(command.UserName);
      // Check valid user
      if (user == null || !await _userManager.CheckPasswordAsync(user, command.Password)) {
        return ServiceResult<AuthResultDto>.Failure([
            "Invalid email or password."
        ]);
      }

      // Generate JWT token
      var accessToken = _jwtTokenManager.GenerateJwtToken(new JwtUserClaims(user.Id, user.UserName!, user.Email!, user.IsDemoUser), 30);

      await _jwtTokenManager.UpsertDeviceAuthInfoAsync(command.RefreshToken, command.DeviceId, user.Id);

      var isTelegramConnected = await _jenainAuthRepository.IsTelegramConnectedAsync(user.Id);


      var authResultDto = new AuthResultDto {
        AccessToken = accessToken,
        AccessTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(30),
        RefreshToken = command.RefreshToken,
        DeviceId = command.DeviceId.ToString(),
        User = new UserDto {
          Id = user.Id,
          UserName = user.UserName!,
          Email = user.Email!,
          IsTelegramConnected = isTelegramConnected

        }
      };


      return ServiceResult<AuthResultDto>.Success(authResultDto);
    }

    public async Task<ServiceResult<bool>> LogoutAsync(LogoutCommand? command, CancellationToken cancellationToken) {
      // If command is null, we can't revoke any specific token, but we can consider the logout successful.
      if (command == null) {
        return ServiceResult<bool>.Success(true);
      }

      var deviceIdGuid = Guid.TryParse(command.DeviceId, out var tempGuid) ? tempGuid : (Guid?)null;

      if (command.RefreshToken != null && deviceIdGuid.HasValue && !string.IsNullOrEmpty(command.UserId)) {
        if (await _jwtTokenManager.DeviceAuthInfoExistsAsync(command.RefreshToken, deviceIdGuid.Value, command.UserId)) {
          await _jwtTokenManager.RevokeDeviceAuthInfoAsync(command.RefreshToken, deviceIdGuid.Value, command.UserId);
        }
      } else {
        return ServiceResult<bool>.Failure(["No matching device auth info found."]);
      }

      return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<AuthResultDto>> RefreshTokenAsync(RefreshTokenCommand command, CancellationToken cancellationToken) {
      // 1. Check for missing cookies
      if (string.IsNullOrEmpty(command.RefreshToken) || string.IsNullOrEmpty(command.DeviceId)) {
        _logger.LogWarning("Refresh attempt failed: Missing cookies (refreshToken={HasRefreshToken}, deviceId={HasDeviceId})",
          !string.IsNullOrEmpty(command.RefreshToken), !string.IsNullOrEmpty(command.DeviceId));
        return ServiceResult<AuthResultDto>.Failure(
          ["Session Expired. Please log in again"]
          );
      }

      Guid? deviceIdGuid = Guid.TryParse(command.DeviceId, out var guid) ? guid : null;

      if (deviceIdGuid is null) {
        return ServiceResult<AuthResultDto>.Failure(
          ["DeviceId is null"]);
      }

      var userId = await _jwtTokenManager.GetUserIdByDeviceAuthAsync(command.RefreshToken, deviceIdGuid.Value);
      if (userId == null) return ServiceResult<AuthResultDto>.Failure(
          ["Invalid Session"]);

      var hasAuthInfo = await _jwtTokenManager.DeviceAuthInfoExistsAsync(command.RefreshToken, deviceIdGuid.Value, userId);
      if (!hasAuthInfo) {
        return ServiceResult<AuthResultDto>.Failure(
          ["Invalid Refresh Token"]);
      } else {
        var isExpired = await _jwtTokenManager.IsRefreshTokenExpiredAsync(command.RefreshToken);
        if (isExpired) {
          await _jwtTokenManager.RevokeDeviceAuthInfoAsync(command.RefreshToken, deviceIdGuid.Value, userId);
          return ServiceResult<AuthResultDto>.Failure(
            ["Refresh Token Expired. Please log in again"]);
        }
      }

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return ServiceResult<AuthResultDto>.Failure(
          ["User no longer exists"]);

      DateTimeOffset? demoExpiresAtUtc = null;

      if (user.IsDemoUser) {
        if (user.DemoStatus == DemoAccountStatus.PendingDeletion) {
          return ServiceResult<AuthResultDto>.Failure(
            ["Demo account is pending deletion"]);
        }

        if (!user.DemoExpiresAtUtc.HasValue ||
            DateTimeOffset.UtcNow > user.DemoExpiresAtUtc.Value) {
          return ServiceResult<AuthResultDto>.Failure(
            ["Demo account has expired"]);
        }

        demoExpiresAtUtc = user.DemoExpiresAtUtc.Value;
      }

      // Update the last used time of the refresh token to extend its validity
      // TODO: using the same refresh token -> securer if generate a new refresh token, implemented later
      await _jwtTokenManager.UpdateDeviceAuthInfoAsync(command.RefreshToken, deviceIdGuid.Value, userId);

      var normalExpiration = DateTimeOffset.UtcNow.AddMinutes(30);
      var accessTokenExpiration = demoExpiresAtUtc.HasValue
        ? new[] { normalExpiration, demoExpiresAtUtc.Value }.Min()
        : normalExpiration;

      var newAccessToken = _jwtTokenManager.GenerateJwtToken(
        new JwtUserClaims(user.Id, user.UserName!, user.Email!, user.IsDemoUser),
        accessTokenExpiration);

      var isTelegramConnected = await _jenainAuthRepository.IsTelegramConnectedAsync(user.Id);

      var AuthResultDto = new AuthResultDto {
        AccessToken = newAccessToken,
        AccessTokenExpiresAtUtc = accessTokenExpiration,
        DeviceId = deviceIdGuid.Value.ToString(),
        User = new UserDto {
          Id = user.Id,
          UserName = user.UserName!,
          Email = user.Email!,
          IsTelegramConnected = isTelegramConnected

        }
      };

      return ServiceResult<AuthResultDto>.Success(AuthResultDto);

    }

    public async Task<ServiceResult<RegisterResultDto>> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken) {

      if (!IsValidInviteToken(
        command.InviteToken,
        _registrationOptions.InviteToken)) {
        return ServiceResult<RegisterResultDto>.Failure(["Invalid invite token"]);
      }

      if (command.Password != command.ConfirmPassword) {
        return ServiceResult<RegisterResultDto>.Failure(["Password and Confirm Password does not match"]);
      }


      var newUser = new ApplicationUser() {
        UserName = command.UserName,
        Email = command.Email,
      };

      // Register user
      var identityResult = await _userManager.CreateAsync(newUser, command.Password);

      if (!identityResult.Succeeded) {
        //return BadRequest(identityResult.Errors);
        var errors = identityResult.Errors
                    .Select(error => error.Description)
                    .ToList();

        return ServiceResult<RegisterResultDto>.Failure(errors);
      }

      return ServiceResult<RegisterResultDto>.Success(new RegisterResultDto { message = "Registered succesffully" });
    }

    public async Task<ServiceResult<RequestResetPasswordDto>> RequestPasswordResetAsync(string email, CancellationToken cancellationToken) {
      var user = await _userManager.FindByEmailAsync(email);
      if (user is null) {
        return ServiceResult<RequestResetPasswordDto>.Success(new RequestResetPasswordDto { Message = string.Empty });
      }

      var token = await _userManager.GeneratePasswordResetTokenAsync(user);

      // will need to send token to user via email using {token}

      return ServiceResult<RequestResetPasswordDto>.Success(new RequestResetPasswordDto { Message = "If an account with that email exists, a password reset link has been sent." });
    }

    public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken) {

      var user = await _userManager.FindByEmailAsync(command.UserEmail);

      if (user == null) {
        return ServiceResult<bool>.Failure(["Invalid password reset request."]);
      }

      // Can decode from Frontend
      var decodedToken = HttpUtility.UrlDecode(command.EmailToken);

      var res = await _userManager.ResetPasswordAsync(user, decodedToken, command.NewPassword);

      if (res == null) {
        return ServiceResult<bool>.Failure(["An error occurred while resetting the password. Please try again."]);
      }

      return res.Succeeded
        ? ServiceResult<bool>.Success(true)
        : ServiceResult<bool>.Failure(res.Errors.Select(e => e.Description).ToList());

    }



    private static bool IsValidInviteToken(
    string? suppliedToken,
    string? configuredToken) {
      if (string.IsNullOrWhiteSpace(suppliedToken) ||
          string.IsNullOrWhiteSpace(configuredToken)) {
        return false;
      }

      byte[] suppliedHash = SHA256.HashData(
          Encoding.UTF8.GetBytes(suppliedToken));

      byte[] configuredHash = SHA256.HashData(
          Encoding.UTF8.GetBytes(configuredToken));

      return CryptographicOperations.FixedTimeEquals(
          suppliedHash,
          configuredHash);
    }
  }
}
