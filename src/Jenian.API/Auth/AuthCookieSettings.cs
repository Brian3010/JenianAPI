namespace Jenian.API.Auth
{
  public sealed class AuthCookieSettings
  {
    public int AccessTokenMinutes { get; init; }
    public int RefreshTokenDays { get; init; }
    public int DeviceIdDays { get; init; }
  }
}
