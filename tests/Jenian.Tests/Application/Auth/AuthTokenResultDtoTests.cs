using Jenian.Application.Features.Auth.Dtos;

namespace Jenian.Tests.Application.Auth
{
  public class AuthTokenResultDtoTests
  {
    [Fact]
    public void Create_WithCompleteTokenResult_PreservesCredentialsAndAbsoluteExpiries() {
      var accessExpiry = new DateTimeOffset(2026, 11, 14, 1, 15, 0, TimeSpan.Zero);
      var sessionDeadline = new DateTimeOffset(2026, 11, 21, 1, 0, 0, TimeSpan.Zero);
      var deviceId = Guid.Parse("574d704a-83b9-48ca-a258-34f8945af533");

      var result = new AuthTokenResultDto {
        AccessToken = "test-access-token",
        AccessTokenExpiresAtUtc = accessExpiry,
        RefreshToken = "test-refresh-token",
        SessionDeadlineUtc = sessionDeadline,
        DeviceId = deviceId
      };

      Assert.Equal("test-access-token", result.AccessToken);
      Assert.Equal(accessExpiry, result.AccessTokenExpiresAtUtc);
      Assert.Equal("test-refresh-token", result.RefreshToken);
      Assert.Equal(sessionDeadline, result.SessionDeadlineUtc);
      Assert.Equal(deviceId, result.DeviceId);
    }
  }
}
