using JenianAPI.Models.AuthModels;
using Microsoft.AspNetCore.Identity;

namespace JenianAPI.Services.Interfaces
{
  public interface IJwtTokenManager
  {
    public string GenerateJwtToken(ApplicationUser user, int TTLInMinute = 5); // short-lived access token


    public string GenerateRefreshToken();

    //private Task StoreRefreshToken(string refreshToken, string? deviceName, string? deviceIpAddress, string userId);


    public Task UpdateRefreshToken(string refreshToken, string deviceName, string? deviceIpAddress, string userId);

    public Task<bool> IsRefreshTokenExists(string refreshToken, string deviceName, string deviceIpAddress, string userId);

    public Task UpdateOrStoreRefreshtoken(string refreshToken, string deviceName, string deviceIpAddress, string userId);

    public Task RevokeRefreshToken(string refreshToken, string deviceName, string deviceIpAddress, string userId);

    //public Task<bool> IsValidRefreshToken(string refreshToken, string deviceName, string deviceIpAddress, string userId);

    public Task<string?> GetUserIdByRefreshTokenAsync(string refreshToken, string deviceName);

  }
}
