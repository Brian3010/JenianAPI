using Jenian.API.Contracts.Auth;
using Jenian.Application.Abstractions.Auth;
using Jenian.Application.Abstractions.Persistence;
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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenManager _jwtTokenManager;
    private readonly ILogger<AuthController> _logger;
    private readonly IJenianAuthRepository _jenainAuthRepository;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenManager jwtTokenManager, ILogger<AuthController> logger, IJenianAuthRepository jenainAuthRepository) {
      _userManager = userManager;
      _jwtTokenManager = jwtTokenManager;
      _logger = logger;
      _jenainAuthRepository = jenainAuthRepository;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto registerRequest) {
      // Check matching passwords
      if (registerRequest.Password != registerRequest.ConfirmPassword) {
        return BadRequest("Password and Confirm Password does not match");
      }


      var newUser = new ApplicationUser() {
        UserName = registerRequest.UserName,
        Email = registerRequest.Email,
      };

      // Register user
      var identityResult = await _userManager.CreateAsync(newUser, registerRequest.Password);

      if (!identityResult.Succeeded) {
        return BadRequest(identityResult.Errors);
      }
      return Ok("Registered succesffully");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest) {

      var user = await _userManager.FindByEmailAsync(loginRequest.UserName) ?? await _userManager.FindByNameAsync(loginRequest.UserName);
      // Check valid user
      if (user == null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password)) {
        return Unauthorized(new { message = "Invalid username or password" });
      }

      var deviceIdCookie = Request.Cookies["deviceId"];
      Guid deviceId = Guid.TryParse(deviceIdCookie, out var guid) ? guid : Guid.NewGuid();
      var refreshToken = Request.Cookies["refreshToken"] ?? _jwtTokenManager.GenerateRefreshToken();
      _logger.LogInformation("Cookie received: deviceId={DeviceId} refreshToken=[redacted]", deviceId);

      // Generate accessToken and refreshToken
      var accessToken = _jwtTokenManager.GenerateJwtToken(new JwtUserClaims(user.Id, user.UserName!, user.Email!), 30);

      await _jwtTokenManager.UpsertDeviceAuthInfoAsync(refreshToken, deviceId, user.Id);

      Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions {
        HttpOnly = true,
        Secure = true, // only over HTTPS
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(30)
      });


      Response.Cookies.Append("deviceId", deviceId.ToString(), new CookieOptions {
        HttpOnly = true,
        Secure = true, // only over HTTPS
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(60)
      });

      // Create a response
      var response = new {
        Message = "Login Successfully",
        AccessToken = accessToken,
        User = new UserDto { Id = user.Id, Email = user.Email, UserName = user.UserName },
      };

      return Ok(response);

    }

    [Authorize]
    [HttpDelete("logout")]
    public async Task<IActionResult> Logout() {
      _logger.LogInformation("Logout API hit");

      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      var refreshToken = Request.Cookies["refreshToken"];
      string? deviceIdCookie = Request.Cookies["deviceId"];
      Guid? deviceId = Guid.TryParse(deviceIdCookie, out var guid) ? guid : null;


      if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(refreshToken) && deviceId.HasValue) {
        var actualDeviceId = deviceId.Value;
        _logger.LogInformation("Logout: userId: {UserId}, refreshToken=[redacted], deviceId {DeviceId}", userId, deviceId);

        var deviceAuthInfoExist = await _jwtTokenManager.DeviceAuthInfoExistsAsync(refreshToken, actualDeviceId, userId);

        _logger.LogInformation("deviceAuthInfoExist {0}", deviceAuthInfoExist);

        if (deviceAuthInfoExist)
          await _jwtTokenManager.RevokeDeviceAuthInfoAsync(refreshToken, actualDeviceId, userId);
      }

      _logger.LogInformation("Logout processed: userId={UserId}, refreshToken=[redacted], deviceId={DeviceId}", userId, deviceId);

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
      if (user == null) return NotFound("User not found");

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
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto resetPasswordRequestDto) {
      var userEmail = resetPasswordRequestDto.UserEmail;

      var user = await _userManager.FindByEmailAsync(userEmail);

      if (user == null) {
        return NotFound("User Not Found");
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

      // 1. Check for missing cookies
      if (string.IsNullOrEmpty(refreshTokenCookie) || string.IsNullOrEmpty(deviceIdCookie)) {
        _logger.LogWarning("Refresh attempt failed: Missing cookies (refreshToken={HasRefreshToken}, deviceId={HasDeviceId})",
          !string.IsNullOrEmpty(refreshTokenCookie), !string.IsNullOrEmpty(deviceIdCookie));
        return Unauthorized("Session expired. Please login again.");
      }

      Guid? deviceId = Guid.TryParse(deviceIdCookie, out var guid) ? guid : null;

      if (deviceId is null) {
        return BadRequest("DeviceId is null");
      }

      var userId = await _jwtTokenManager.GetUserIdByDeviceAuthAsync(refreshTokenCookie, deviceId.Value);
      if (userId == null) return Unauthorized("Invalid session.");

      if (!await _jwtTokenManager.DeviceAuthInfoExistsAsync(refreshTokenCookie, deviceId.Value, userId)) {
        return Unauthorized("Invalid Refresh Token");
      }

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return Unauthorized("User no longer exists.");

      var newAccessToken = _jwtTokenManager.GenerateJwtToken(new JwtUserClaims(user.Id, user.UserName!, user.Email!), 30);


      // Create a response
      var response = new {
        Message = "Auth session (refreshToken - deviceId) processed successfully",
        AccessToken = newAccessToken,
        User = new UserDto { Id = user.Id, Email = user.Email, UserName = user.UserName },
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
