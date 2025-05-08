using JenianAPI.Dtos;
using JenianAPI.Models.AuthModels;
using JenianAPI.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JenianAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AuthController : ControllerBase
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenManager _jwtTokenManager;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenManager jwtTokenManager) {
      _userManager = userManager;
      _jwtTokenManager = jwtTokenManager;
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
        return Unauthorized("Invalid username or password");
      }

      // Device information
      var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();


      // Generate accessToken and refreshToken
      var accessToken = _jwtTokenManager.GenerateJwtToken(user, 5);
      var refreshToken = _jwtTokenManager.GenerateRefreshToken();

      // Store refresh-token to database
      await _jwtTokenManager.StoreRefreshToken(refreshToken, null, ipAddress, user.Id);



      // Set HttpOnly cookie
      var cookieOptions = new CookieOptions {
        HttpOnly = true,
        Secure = true, // only over HTTPS
        SameSite = SameSiteMode.Strict,
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


    //[HttpPost("logout")]
    //public async Task<IActionResult> Logout() {

    //}










  }
}
