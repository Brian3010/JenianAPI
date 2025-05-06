using Microsoft.AspNetCore.Identity;

namespace JenianAPI.Services.Interfaces
{
  public interface IJwtTokenManager
  {
    public string GenerateJwtToken(IdentityUser user, int TTLInMinute = 5);


    public string GenerateRefreshToken();

    // Working on Genereate JWT Bearer token

  }
}
