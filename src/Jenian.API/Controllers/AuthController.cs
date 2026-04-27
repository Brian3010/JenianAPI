using Jenian.API.Contracts.Auth;
using Jenian.Application.Abstractions.Auth;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Features.Auth.Commands;
using Jenian.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Web;

namespace Jenian.API.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AuthController : ControllerBase
  {
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenManager _jwtTokenManager;
    private readonly ILogger<AuthController> _logger;
    private readonly IJenianAuthRepository _jenainAuthRepository;

    public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager, IJwtTokenManager jwtTokenManager, ILogger<AuthController> logger, IJenianAuthRepository jenainAuthRepository) {
      _authService = authService;
      _userManager = userManager;
      _jwtTokenManager = jwtTokenManager;
      _logger = logger;
      _jenainAuthRepository = jenainAuthRepository;
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


      Response.Cookies.Append("refreshToken", loginResult.Data!.RefreshToken, new CookieOptions {
        HttpOnly = true,
        Secure = true, // only over HTTPS
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(30)
      });


      Response.Cookies.Append("deviceId", loginResult.Data.DeviceId, new CookieOptions {
        HttpOnly = true,
        Secure = true, // only over HTTPS
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(60)
      });

      return Ok(new LoginResponse {
        Message = "Login Successfully",
        AccessToken = loginResult.Data.AccessToken,
        User = loginResult.Data.User,
      });

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
      Response.Cookies.Append("refreshToken", "", new CookieOptions {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(-1), // Set expiration in the past
      });

      // remove DeviceId cookie
      Response.Cookies.Append("deviceId", "", new CookieOptions {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(-1), // Set expiration in the past
      });

      return Ok("Logged out successfully.");
    }

    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] string email) {

      var user = await _userManager.FindByEmailAsync(email);
      // Always return the same response to prevent email enumeration
      if (user == null) {
        return Ok(new { message = "If that email is registered, a reset token will be provided." });
      }

      var token = await _userManager.GeneratePasswordResetTokenAsync(user);

      var encodedToken = HttpUtility.UrlEncode(token);

      /* //TODO: Will need to send a link via email asking user to fill a form and hit POST reset-password
       * to reset password
       * 
       * For now, This API will send back a random token to use for reseting password
       */

      return Ok(new { ResetToken = encodedToken });
      ;

    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequestDto) {
      var userEmail = resetPasswordRequestDto.UserEmail;

      var user = await _userManager.FindByEmailAsync(userEmail);

      if (user == null) {
        return BadRequest("Invalid password reset request.");
      }

      // Can decode from Frontend
      var decodedToken = HttpUtility.UrlDecode(resetPasswordRequestDto.EmailToken);

      var res = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordRequestDto.NewPassword);

      if (!res.Succeeded)
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

      var refreshTokenResult = await _authService.RefreshTokenAsync(refreshTokenCommand, CancellationToken.None);

      if (!refreshTokenResult.IsSuccess)
        return Unauthorized(refreshTokenResult.Errors);


      // Create a response
      var response = new {
        Message = "Auth session (refreshToken - deviceId) processed successfully",
        refreshTokenResult.Data!.AccessToken,
        refreshTokenResult.Data.User,
      };

      return Ok(response);
    }

    [Authorize]
    [HttpGet("get-me")]
    public async Task<IActionResult> getMe() {
      // JWT is already validated + "decoded" into claims
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (userId == null) return NotFound("GET: get-me - cannot find userId");

      var username = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value;
      var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
      var isTelegramConnected = await _jenainAuthRepository.IsTelegramConnectedAsync(userId);
      return Ok(new { username, isTelegramConnected, email });
    }


    // GET /api/auth/sessions	- List all active sessions/devices (from refresh tokens)




  }
}
