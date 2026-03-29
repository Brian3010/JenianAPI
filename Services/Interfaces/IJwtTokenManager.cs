using JenianAPI.Models.AuthModels;
using Microsoft.AspNetCore.Identity;

namespace JenianAPI.Services.Interfaces
{
    public interface IJwtTokenManager
    {
        public string GenerateJwtToken(ApplicationUser user, int TTLInMinute = 5); // short-lived access token


        public string GenerateRefreshToken();

        //private Task StoreRefreshToken(string refreshToken, string? deviceName, string? deviceIpAddress, string userId);

        public Task UpdateDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId);

        public Task StoreDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId);

        public Task<bool> DeviceAuthInfoExistsAsync(string refreshToken, Guid deviceId, string userId);

        public Task UpsertDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId);

        public Task RevokeDeviceAuthInfoAsync(string refreshToken, Guid deviceId, string userId);

        //public Task<bool> IsValidRefreshToken(string refreshToken, string deviceName, string deviceIpAddress, string userId);

        public Task<string?> GetUserIdByDeviceAuthAsync(string refreshToken, Guid deviceId);

    }
}
