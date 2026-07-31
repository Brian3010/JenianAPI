using Jenian.API.Auth;
using Jenian.API.Contracts.Auth;
using Jenian.API.Contracts.Common;
using Jenian.Application.Abstractions.Auth;
using Jenian.Application.Abstractions.DemoAccount;
using Jenian.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        private readonly IDemoAccountService _demoAccountService;

        public AuthController(IAuthService authService,
          IJwtTokenManager jwtTokenManager,
          ILogger<AuthController> logger,
          IOptions<AuthCookieSettings> AuthCookieOptions,
          IDemoAccountService demoAccountService
          ) {
            _authService = authService;
            _jwtTokenManager = jwtTokenManager;
            _logger = logger;
            _authCookieOptions = AuthCookieOptions;
            _demoAccountService = demoAccountService;
        }


        [HttpDelete("demo-logout")]
        [Authorize]
        public async Task<IActionResult> DemoLogout(CancellationToken cancellationToken) {

            var deviceIdCookie = Request.Cookies["deviceId"];
            Guid deviceId = Guid.TryParse(deviceIdCookie, out var guid) ? guid : Guid.NewGuid();
            var refreshToken = Request.Cookies["refreshToken"] ?? _jwtTokenManager.GenerateRefreshToken();
            _logger.LogInformation("Cookie received: deviceId={DeviceId} refreshToken=[redacted]", deviceId);

            var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userId)) {
                return Unauthorized();
            }

            var result = await _demoAccountService.EndDemoSessionAsync(userId, refreshToken, deviceId, cancellationToken);
            if (!result.IsSuccess) {
                return BadRequest(ApiResponse<object>.Fail(result.Errors));
            }

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

            return NoContent(); // 204 No Content
        }

        [HttpPost("demo-login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> DemoLogin(CancellationToken cancellationToken) {
            var deviceIdCookie = Request.Cookies[AuthCookieNames.DeviceId];
            Guid deviceId = Guid.TryParse(deviceIdCookie, out var guid) ? guid : Guid.NewGuid();
            var refreshToken = Request.Cookies[AuthCookieNames.RefreshToken] ?? _jwtTokenManager.GenerateRefreshToken();
            _logger.LogInformation("Cookie received: deviceId={DeviceId} refreshToken=[redacted]", deviceId);

            var result = await _demoAccountService.CreateDemoSessionAsync(refreshToken, deviceId, cancellationToken);

            if (!result.IsSuccess || result.Data == null) {
                return BadRequest(ApiResponse<DemoLoginResult>.Fail(result.Errors));
            }

            var ONE_HOUR_EXPIRATION = DateTime.UtcNow.AddHours(1);

            // set cookies for access token, refresh token, and device id
            Response.Cookies.Append(AuthCookieNames.RefreshToken, result.Data.RefreshToken!, new CookieOptions {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = ONE_HOUR_EXPIRATION
            });


            Response.Cookies.Append(AuthCookieNames.DeviceId, deviceId.ToString(), new CookieOptions {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = ONE_HOUR_EXPIRATION
            });

            Response.Cookies.Append(AuthCookieNames.AccessToken, result.Data.AccessToken, new CookieOptions {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = result.Data.AccessTokenExpiresAtUtc
            });

            return NoContent();


            //return Ok(ApiResponse<DemoLoginResult>.Ok(result.Data));
        }

        [HttpPost("register")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest, CancellationToken cancellationToken) {

            var command = new RegisterCommand {
                ConfirmPassword = registerRequest.ConfirmPassword,
                Password = registerRequest.Password,
                Email = registerRequest.Email,
                UserName = registerRequest.UserName,
                SecretToken = registerRequest.SecretToken
            };
            var result = await _authService.RegisterAsync(command, cancellationToken);

            if (!result.IsSuccess) {
                return BadRequest(ApiResponse<object>.Fail(result.Errors));
            }

            return Ok(ApiResponse<object>.Ok(new { message = "Registered successfully." }));
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest, CancellationToken cancellationToken) {

            var deviceIdCookie = Request.Cookies[AuthCookieNames.DeviceId];
            Guid deviceId = Guid.TryParse(deviceIdCookie, out var guid) ? guid : Guid.NewGuid();
            var refreshToken = Request.Cookies[AuthCookieNames.RefreshToken] ?? _jwtTokenManager.GenerateRefreshToken();
            _logger.LogInformation("Cookie received: deviceId={DeviceId} refreshToken=[redacted]", deviceId);

            var loginCommand = new LoginCommand {
                UserName = loginRequest.UserName,
                Password = loginRequest.Password,
                DeviceId = deviceId,
                RefreshToken = refreshToken
            };

            var loginResult = await _authService.LoginAsync(loginCommand, cancellationToken);

            if (!loginResult.IsSuccess) {
                return Unauthorized(ApiResponse<object>.Fail(loginResult.Errors));
            }


            Response.Cookies.Append(AuthCookieNames.RefreshToken, loginResult.Data!.RefreshToken!, new CookieOptions {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(_authCookieOptions.Value.RefreshTokenDays)
            });


            Response.Cookies.Append(AuthCookieNames.DeviceId, loginResult.Data.DeviceId, new CookieOptions {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(_authCookieOptions.Value.DeviceIdDays)
            });

            Response.Cookies.Append(AuthCookieNames.AccessToken, loginResult.Data.AccessToken, new CookieOptions {
                HttpOnly = true,
                Secure = true,
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
            var refreshTokenCookie = Request.Cookies[AuthCookieNames.RefreshToken];
            var deviceIdCookie = Request.Cookies[AuthCookieNames.DeviceId];
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

            return NoContent();
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] string email) {

            var resetToken = await _authService.RequestPasswordResetAsync(email, CancellationToken.None);

            /* //TODO: Will need to send a link via email asking user to fill a form and hit POST reset-password
             * to reset password
             * 
             * For now, This API will send back a random token to use for reseting password
             */

            return Ok(ApiResponse<object>.Ok(new { ResetToken = resetToken.Data! }));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequestDto, CancellationToken cancellationToken) {


            var command = new ResetPasswordCommand {
                UserEmail = resetPasswordRequestDto.UserEmail,
                NewPassword = resetPasswordRequestDto.NewPassword,
                ConfirmPassword = resetPasswordRequestDto.ConfirmPassword,
                EmailToken = resetPasswordRequestDto.EmailToken
            };

            var res = await _authService.ResetPasswordAsync(command, cancellationToken);

            if (!res.IsSuccess)
                return BadRequest(ApiResponse<object>.Fail(res.Errors));

            return Ok(ApiResponse<object>.Ok(new { message = "Password has been reset successfully." }));

        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken) {

            var refreshTokenCookie = Request.Cookies[AuthCookieNames.RefreshToken];
            string? deviceIdCookie = Request.Cookies[AuthCookieNames.DeviceId];

            var refreshTokenCommand = new RefreshTokenCommand {
                DeviceId = deviceIdCookie,
                RefreshToken = refreshTokenCookie,
            };

            var tokenRes = await _authService.RefreshTokenAsync(refreshTokenCommand, cancellationToken);

            if (!tokenRes.IsSuccess)
                return Unauthorized(ApiResponse<object>.Fail(tokenRes.Errors ?? new[] { "Failed to refresh token." }));

            Response.Cookies.Append(AuthCookieNames.AccessToken, tokenRes.Data!.AccessToken, new CookieOptions {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = tokenRes.Data.AccessTokenExpiresAtUtc,
            });

            return NoContent();
        }

        [Authorize]
        [HttpGet("get-me")]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken) {
            // JWT is already validated + "decoded" into claims
            var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (userId == null) return NotFound(ApiResponse<object>.Fail(["User not found."]));

            var userName = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value;
            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var isDemoUser = User.FindFirst("IsDemoUser")?.Value;
            var telegramConnectionResult = await _authService.HasTelegramConnectedAsync(userId, cancellationToken);
            if (!telegramConnectionResult.IsSuccess) {
                return BadRequest(ApiResponse<object>.Fail(telegramConnectionResult.Errors));
            }
            return Ok(ApiResponse<object>.Ok(new {
                UserName = userName,
                Email = email,
                IsDemoUser = isDemoUser,
                IsTelegramConnected = telegramConnectionResult.Data
            }));
        }


        // GET /api/auth/sessions	- List all active sessions/devices (from refresh tokens)


    }
}
