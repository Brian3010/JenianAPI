using Jenian.Application.Abstractions.Auth;
using Jenian.Infrastructure.Identity;
using Jenian.Infrastructure.Persistence.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Jenian.Infrastructure.Services.Auth
{
  /* JS analogy:
   * - GenerateJwtToken = sign(payload, secret, {expiresIn})
   * - Refresh tokens stored in DB table (like a "sessions" table)
   */
  public class JwtTokenManager : IJwtTokenManager
  {
    private readonly IConfiguration _configuration;
    private readonly JenianAuthDbContext _dbContext;
    private readonly ILogger<JwtTokenManager> _logger;

    public JwtTokenManager(IConfiguration configuration, JenianAuthDbContext dbContext, ILogger<JwtTokenManager> logger) {
      _configuration = configuration;
      _dbContext = dbContext;
      _logger = logger;
    }

    /// <summary>
    /// Generate accessToken
    /// </summary>
    /// <param name="user"></param>
    /// <param name="TTLInMinute"></param>
    /// <returns>A string of jwt token</returns>
    public string GenerateJwtToken(JwtUserClaims user, int TTLInMinute = 5) {
      _logger.LogInformation("GenerateJwtToken with userId: {0}", user.Id);
      var jwt = _configuration.GetSection("jwt");
      var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

      var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

      // provide claims
      var tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity(
          [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id), // NameIdentifier
            new Claim(JwtRegisteredClaimNames.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),

          ]),
        Expires = DateTime.UtcNow.AddMinutes(TTLInMinute),
        SigningCredentials = credentials,
        Issuer = jwt["Issuer"],
        Audience = jwt["Audience"]
      };

      return new JsonWebTokenHandler().CreateToken(tokenDescriptor);

    }

    public string GenerateRefreshToken() {
      return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    /// <summary>
    /// Revoke refreshToken
    /// </summary>
    public async Task RevokeDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId) {

      var rfToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.DeviceId == deviceId && r.UserId == userId && r.Token == refreshToken && !r.IsRevoked);
      _logger.LogInformation("rfToken: {rfToken}", rfToken);


      if (rfToken != null) {
        rfToken.IsRevoked = true;
        rfToken.RevokedAt = DateTime.UtcNow;
      }

      await _dbContext.SaveChangesAsync();
    }


    /// <summary>
    /// Update or store refreshToken or deviceId if any of them change
    /// </summary>
    public async Task UpsertDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId) {

      // Check if token exists with deviceName, deviceIpAddress, userId
      if (await DeviceAuthInfoExistsAsync(refreshToken, deviceId, userId)) {
        // Update
        await UpdateDeviceAuthInfoAsync(refreshToken, deviceId, userId);
      } else {
        // Store
        await StoreDeviceAuthInfoAsync(refreshToken, deviceId, userId);
      }

    }

    /// <summary>
    /// Check if RefreshToken or DeviceId exist.
    /// </summary>
    /// <returns>true (exist) or false (not exist)</returns>
    public async Task<bool> DeviceAuthInfoExistsAsync(string refreshToken, Guid deviceId, string userId) {
      return await _dbContext.RefreshTokens.AnyAsync(rf =>
          (rf.UserId == userId && rf.DeviceId == deviceId) ||
          (rf.Token == refreshToken && !rf.IsRevoked));
    }

    public async Task StoreDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId) {

      _dbContext.RefreshTokens.Add(new RefreshToken {
        Id = Guid.NewGuid(),
        UserId = userId,
        Token = refreshToken,
        CreatedAt = DateTime.UtcNow,
        ExpiredAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
        DeviceId = deviceId,
      });


      await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Update refreshToken or deviceId
    /// </summary>
    public async Task UpdateDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId) {
      var rfToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r =>
        (r.UserId == userId && r.Token == refreshToken) ||
        (r.DeviceId == deviceId && !r.IsRevoked));

      if (rfToken != null) {
        rfToken.Token = refreshToken;
        rfToken.DeviceId = deviceId;
        rfToken.IsRevoked = false;
      }
      await _dbContext.SaveChangesAsync();
    }

    public async Task<string?> GetUserIdByDeviceAuthAsync(string refreshToken, Guid deviceId) {

      var user = await _dbContext.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(r => r.DeviceId == deviceId && !r.IsRevoked && r.Token == refreshToken);

      if (user == null) return null;

      return user.UserId;
    }
  }
}
