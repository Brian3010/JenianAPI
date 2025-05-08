using Microsoft.AspNetCore.Identity;

namespace JenianAPI.Models.AuthModels
{
  public class ApplicationUser : IdentityUser
  {

    public List<RefreshToken> RefreshTokens { get; set; } = new();
  }
}
