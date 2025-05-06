using JenianAPI.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
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

    public JwtTokenManager(IConfiguration configuration) {
      _configuration = configuration;
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
      throw new NotImplementedException();
    }
  }
}
