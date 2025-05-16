using Microsoft.AspNetCore.Identity;

namespace JenianAPI.Services.Interfaces
{
  public interface IJwtTokenManager
  {
    public string GenerateJwtToken(IdentityUser user, int TTLInMinute = 5);


    public string GenerateRefreshToken();

    //private Task StoreRefreshToken(string refreshToken, string? deviceName, string? deviceIpAddress, string userId);


    //public Task UpdateRefreshToken(string refreshToken, string? deviceName, string? deviceIpAddress, string userId);

    public Task<bool> IsRefreshTokenExists(string refreshToken, string deviceName, string deviceIpAddress, string userId);

    public Task UpdateOrStoreRefreshtoken(string refreshToken, string deviceName, string deviceIpAddress, string userId);

    public Task RevokeRefreshToken(string refreshToken, string deviceName, string deviceIpAddress, string userId);
  }
}
