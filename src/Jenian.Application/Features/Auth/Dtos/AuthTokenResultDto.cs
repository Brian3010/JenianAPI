namespace Jenian.Application.Features.Auth.Dtos
{
  public class AuthTokenResultDto
  {
    public required string AccessToken { get; init; }
    public required DateTimeOffset AccessTokenExpiresAtUtc { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset SessionDeadlineUtc { get; init; }
    public required Guid DeviceId { get; init; }



  }
}
