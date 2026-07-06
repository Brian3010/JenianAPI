using Jenian.API.Auth;
using Jenian.API.Contracts.Auth;
using Jenian.API.Contracts.Common;
using Jenian.Application.Abstractions.Auth;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace Jenian.API.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AuthController : ControllerBase
  {
    private readonly IAuthService _authService;
    private readonly IJwtTokenManager _jwtTokenManager;
    private readonly ILogger<AuthController> _logger;
    private readonly IOptions<AuthCookieSettings> _authCookieOptions;

    public AuthController(IAuthService authService,
      IJwtTokenManager jwtTokenManager,
      ILogger<AuthController> logger,
      IJenianAuthRepository jenianAuthRepository,
      IOptions<AuthCookieSettings> AuthCookieOptions
      ) {
      _authService = authService;
      _jwtTokenManager = jwtTokenManager;
      _logger = logger;
      _authCookieOptions = AuthCookieOptions;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest registerRequest) {

      var command = new RegisterCommand {
        ConfirmPassword = registerRequest.ConfirmPassword,
        Password = registerRequest.Password,
        Email = registerRequest.Email,
        UserName = registerRequest.UserName,
      };
      var result = await _authService.RegisterAsync(command, CancellationToken.None);

      if (!result.IsSuccess) {
        return BadRequest(result.Errors);
      }

      return Ok("Registered succesffully");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest) {

      var deviceIdCookie = Request.Cookies["deviceId"];
      Guid deviceId = Guid.TryParse(deviceIdCookie, out var guid) ? guid : Guid.NewGuid();
      var refreshToken = Request.Cookies["refreshToken"] ?? _jwtTokenManager.GenerateRefreshToken();
      _logger.LogInformation("Cookie received: deviceId={DeviceId} refreshToken=[redacted]", deviceId);

      var loginCommand = new LoginCommand {
        UserName = loginRequest.UserName,
        Password = loginRequest.Password,
        DeviceId = deviceId,
        RefreshToken = refreshToken
      };

      var loginResult = await _authService.LoginAsync(loginCommand, CancellationToken.None);

      if (!loginResult.IsSuccess) {
        return Unauthorized(new {
          loginResult.Errors
        });
      }


      Response.Cookies.Append(AuthCookieNames.RefreshToken, loginResult.Data!.RefreshToken!, new CookieOptions {
        HttpOnly = true,
        Secure = true, // only over HTTPS
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(_authCookieOptions.Value.RefreshTokenDays)
      });


      Response.Cookies.Append(AuthCookieNames.DeviceId, loginResult.Data.DeviceId, new CookieOptions {
        HttpOnly = true,
        Secure = true, // only over HTTPS
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(_authCookieOptions.Value.DeviceIdDays)
      });

      Response.Cookies.Append(AuthCookieNames.AccessToken, loginResult.Data.AccessToken, new CookieOptions {
        HttpOnly = true,
        Secure = true, // only over HTTPS
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddMinutes(_authCookieOptions.Value.AccessTokenMinutes)
      });

      return NoContent();
    }

    [Authorize]
    [HttpDelete("logout")]
    public async Task<IActionResult> Logout() {
      _logger.LogInformation("Logout API hit");

      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      var refreshTokenCookie = Request.Cookies["refreshToken"];
      var deviceIdCookie = Request.Cookies["deviceId"];
      //Guid? deviceId = Guid.TryParse(deviceIdCookie, out var guid) ? guid : null;

      var logoutCommand = new LogoutCommand {
        UserId = userId,
        RefreshToken = refreshTokenCookie,
        DeviceId = deviceIdCookie
      };

      await _authService.LogoutAsync(logoutCommand, CancellationToken.None);


      // remove refreshToken cookie
      Response.Cookies.Append(AuthCookieNames.RefreshToken, "", new CookieOptions {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(-1), // Set expiration in the past
      });

      // remove DeviceId cookie
      Response.Cookies.Append(AuthCookieNames.DeviceId, "", new CookieOptions {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(-1), // Set expiration in the past
      });

      Response.Cookies.Append(AuthCookieNames.AccessToken, "", new CookieOptions {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(-1), // Set expiration in the past
      });

      return Ok("Logged out successfully.");
    }

    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] string email) {

      var resetToken = await _authService.RequestPasswordResetAsync(email, CancellationToken.None);

      /* //TODO: Will need to send a link via email asking user to fill a form and hit POST reset-password
       * to reset password
       * 
       * For now, This API will send back a random token to use for reseting password
       */

      return Ok(new { ResetToken = resetToken.Data! });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequestDto) {


      var command = new ResetPasswordCommand {
        UserEmail = resetPasswordRequestDto.UserEmail,
        NewPassword = resetPasswordRequestDto.NewPassword,
        ConfirmPassword = resetPasswordRequestDto.ConfirmPassword,
        EmailToken = resetPasswordRequestDto.EmailToken
      };

      var res = await _authService.ResetPasswordAsync(command, CancellationToken.None);

      if (!res.IsSuccess)
        return BadRequest(res.Errors);

      return Ok("Password has been reset successfully.");

    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken() {

      var refreshTokenCookie = Request.Cookies["refreshToken"];
      string? deviceIdCookie = Request.Cookies["deviceId"];

      var refreshTokenCommand = new RefreshTokenCommand {
        DeviceId = deviceIdCookie,
        RefreshToken = refreshTokenCookie,
      };

      var tokenRes = await _authService.RefreshTokenAsync(refreshTokenCommand, CancellationToken.None);

      if (!tokenRes.IsSuccess)
        return Unauthorized(new { message = tokenRes.Errors });

      Response.Cookies.Append(AuthCookieNames.AccessToken, tokenRes.Data!.AccessToken, new CookieOptions {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddMinutes(_authCookieOptions.Value.AccessTokenMinutes),
      });

      return NoContent();
    }

    [Authorize]
    [HttpGet("get-me")]
    public async Task<IActionResult> getMe() {
      // JWT is already validated + "decoded" into claims
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound("GET: get-me - cannot find userId");

      var userName = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value;
      var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
      var isTelegramConnected = await _authService.HasTelegramConnectedAsync(userId, CancellationToken.None);
      if (!isTelegramConnected.IsSuccess) {
        return BadRequest(new { message = isTelegramConnected.Errors });
      }
      return Ok(new { userName, isTelegramConnected = isTelegramConnected.Data, email });
    }


    // GET /api/auth/sessions	- List all active sessions/devices (from refresh tokens)




  }
}
