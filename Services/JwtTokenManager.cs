using JenianAPI.Data;
using JenianAPI.Models.AuthModels;
using JenianAPI.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace JenianAPI.Services
{
  public class JwtTokenManager : IJwtTokenManager
  {
    private readonly IConfiguration _configuration;
    private readonly JenianAuthDbContext _dbContext;

    public JwtTokenManager(IConfiguration configuration, JenianAuthDbContext dbContext) {
      _configuration = configuration;
      _dbContext = dbContext;
    }
    public string GenerateJwtToken(IdentityUser user, int TTLInMinute = 5) {

      var jwt = _configuration.GetSection("jwt");
      var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

      var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

      // provide claims
      var tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity(
          [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),

          ]),
        Expires = DateTime.UtcNow.AddMinutes(TTLInMinute),
        SigningCredentials = credentials,
        Issuer = jwt["Issuer"],
        Audience = jwt["Audience"]
      };

      return new JsonWebTokenHandler().CreateToken(tokenDescriptor);

    }

    public string GenerateRefreshToken() {
      const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
      StringBuilder result = new StringBuilder();
      Random random = new();

      for (int i = 0; i < 64; i++) {
        result.Append(validChars[random.Next(validChars.Length)]);
      }

      return result.ToString();
    }

    public async Task<bool> IsRefreshTokenExists(string refreshToken, string? deviceName, string deviceIpAddress, string userId) {

      deviceName ??= "Unknown Device";

      var foundToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rf => rf.UserId == userId && rf.DeviceName == deviceName && rf.DeviceIpAddress == deviceIpAddress);

      return foundToken != null;
    }

    public async Task StoreRefreshToken(string refreshToken, string? deviceName, string? deviceIpAddress, string userId) {

      _dbContext.RefreshTokens.Add(new RefreshToken {
        Id = new Guid(),
        UserId = userId,
        Token = refreshToken,
        CreatedAt = DateTime.UtcNow,
        ExpiredAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
        DeviceName = deviceName ??= "Unknown Device",
        DeviceIpAddress = deviceIpAddress ?? "localhost",
      });


      await _dbContext.SaveChangesAsync();
    }

    public Task UpdateRefreshToken(string refreshToken, string? deviceName, string? deviceIpAddress, string userId) {



      //


      throw new NotImplementedException();
    }



  }
}
