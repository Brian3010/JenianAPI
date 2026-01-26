using JenianAPI.Dtos.AuthDtos;
using JenianAPI.Helpers;
using JenianAPI.Models.AuthModels;
using JenianAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Web;

namespace JenianAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AuthController : ControllerBase
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenManager _jwtTokenManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenManager jwtTokenManager, ILogger<AuthController> logger) {
      _userManager = userManager;
      _jwtTokenManager = jwtTokenManager;
      _logger = logger;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto registerRequest) {
      // Check matching passwords
      if (registerRequest.Password != registerRequest.ConfirmPassword) {
        return BadRequest("Password and Confirm Password does not match");
      }


      var newUser = new ApplicationUser() {
        UserName = registerRequest.Email,
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
      var user = await _userManager.FindByEmailAsync(loginRequest.Email);
      // Check valid user
      if (user == null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password)) {
        return Unauthorized(new { message = "Invalid username or password" });
      }

      // IP Address
      var ipAddress = IpHelper.GetClientIp(HttpContext);

      var deviceId = Request.Cookies["deviceId"];
      if (deviceId == null) return NotFound(new {message="Cannot find deviceID" });

      // Generate accessToken and refreshToken
      var accessToken = _jwtTokenManager.GenerateJwtToken(user, 5);
      var refreshToken = _jwtTokenManager.GenerateRefreshToken();

      await _jwtTokenManager.UpdateOrStoreRefreshtoken(refreshToken, deviceId, ipAddress, user.Id);

      // Set HttpOnly cookie
      var cookieOptions = new CookieOptions {
        HttpOnly = true,
        Secure = true, // only over HTTPS
        //SameSite = SameSiteMode.Strict,
        SameSite = SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(7),
        //Path = "/api/auth/refresh" // Optional: limit path
      };

      Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

      // Create a response
      var response = new {
        Message = "Login Successfully",
        AccessToken = accessToken,
        User = new UserDto { Id = user.Id, Email = user.Email, UserName = user.UserName },
      };

      return Ok(response);

    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto logoutRequest) {
      /**
       * revoke refresh token
       * delete cookies
       */

      // Refresh token from cookie
      var refreshToken = Request.Cookies["refreshToken"];
      if (string.IsNullOrEmpty(refreshToken)) return Unauthorized("Refresh token not found");

      // IP Address
      var ipAddress = IpHelper.GetClientIp(HttpContext);

      // Check if token exist before continure proceed
      if (!await _jwtTokenManager.IsRefreshTokenExists(refreshToken, logoutRequest.DeviceName, ipAddress!, logoutRequest.UserId)) {
        return Unauthorized("Some values not exist");
      }

      await _jwtTokenManager.RevokeRefreshToken(refreshToken, logoutRequest.DeviceName, ipAddress!, logoutRequest.UserId);


      Response.Cookies.Append("refreshToken", "", new CookieOptions {
        HttpOnly = true,
        Secure = true,
        //SameSite = SameSiteMode.Strict,
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
      /*
       * Find the user email in the database -> wait for confirmation email
       * return not found user
       * 
       * replace password
       */
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
    //public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto refreshTokenRequestDto) {
    public async Task<IActionResult> RefreshToken() {
      /** Check if refresh token is revoked 
       * if yes, return invalid token.
       * 
       * If no,
       * Generate a new accessToken
       * Return same repsonse as the log-in API
       */

      // IP Address
      var ipAddress = IpHelper.GetClientIp(HttpContext); // Could be a problem in the future implemntation ??
      //_logger.LogInformation("ipAddress: {ipAddress} ", ipAddress);

      var refreshToken = Request.Cookies["refreshToken"];
      if (refreshToken == null) return NotFound("Cannot find refresh token");

      var deviceId = Request.Cookies["deviceId"];
      if (deviceId == null) return NotFound("Cannot find device ID");

      var userId = await _jwtTokenManager.GetUserIdByRefreshTokenAsync(refreshToken, deviceId);
      if (userId == null) return NotFound("Cannot find user ID");

      if (!await _jwtTokenManager.IsRefreshTokenExists(refreshToken, deviceId, ipAddress, userId)) {
        return Unauthorized("Invalid Refresh Token");
      }

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return NotFound("User not exist");

      var newAccessToken = _jwtTokenManager.GenerateJwtToken(user);


      // Create a response
      var response = new {
        Message = "Login Successfully",
        AccessToken = newAccessToken,
        User = new UserDto { Id = user.Id, Email = user.Email, UserName = user.UserName },
      };

      return Ok(response);
    }

    [Authorize]
    [HttpGet("get-me")]
    public async Task<IActionResult> getMe() {
      // JWT is already validated + “decoded” into claims
      var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      var username = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value;
      var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
      //var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
      //_logger.LogInformation("claims: {0}", claims);
      //return Ok(claims);
      return Ok(new { userId, username, email });
    } 



    // GET /api/auth/sessions	- List all active sessions/devices (from refresh tokens)





  }
}
