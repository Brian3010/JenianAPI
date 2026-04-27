namespace Jenian.API.Contracts.Auth
{
  public class RefreshTokenRequest
  {
    public required string UserId { get; set; }

    public required string DeviceName { get; set; }

  }
}
