using Jenian.API.Contracts.Auth;
using Jenian.Application.Abstractions.Auth;
using Jenian.Application.Common;
using Jenian.Application.Features.Auth.Commands;
using Jenian.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace Jenian.Infrastructure.Identity
{
  public class AuthService : IAuthService
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenManager _jwtTokenManager;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserManager<ApplicationUser> userManager,
      IJwtTokenManager jwtTokenManager,
      ILogger<AuthService> logger
      ) {
      _userManager = userManager;
      _jwtTokenManager = jwtTokenManager;
      _logger = logger;
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
      var accessToken = _jwtTokenManager.GenerateJwtToken(new JwtUserClaims(user.Id, user.UserName!, user.Email!), 30);

      await _jwtTokenManager.UpsertDeviceAuthInfoAsync(command.RefreshToken, command.DeviceId, user.Id);


      var authResultDto = new AuthResultDto {
        AccessToken = accessToken,
        RefreshToken = command.RefreshToken,
        DeviceId = command.DeviceId.ToString(),
        User = new UserDto {
          Id = user.Id,
          UserName = user.UserName!,
          Email = user.Email!
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

      if (!await _jwtTokenManager.DeviceAuthInfoExistsAsync(command.RefreshToken, deviceIdGuid.Value, userId)) {
        return ServiceResult<AuthResultDto>.Failure(
          ["Invalid Refresh Token"]);
      }

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return ServiceResult<AuthResultDto>.Failure(
          ["User no longer exists"]);

      var newAccessToken = _jwtTokenManager.GenerateJwtToken(new JwtUserClaims(user.Id, user.UserName!, user.Email!), 30);


      var AuthResultDto = new AuthResultDto {
        AccessToken = newAccessToken,
        DeviceId = deviceIdGuid.Value.ToString(),
        User = new UserDto {
          Id = user.Id,
          UserName = user.UserName!,
          Email = user.Email!
        }
      };

      return ServiceResult<AuthResultDto>.Success(AuthResultDto);

    }

    public async Task<ServiceResult<RegisterResultDto>> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken) {
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

    public Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken) {
      throw new NotImplementedException();
    }
  }
}
