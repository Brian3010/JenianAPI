namespace Jenian.Application.Abstractions.Auth
{
  /// <summary>
  /// A matched refresh credential and its owning session.
  /// </summary>
  public class AuthCredentialSnapshot
  {
    public required AuthSessionSnapshot Session { get; init; }
    public required AuthRefreshTokenSnapshot RefreshToken { get; init; }
  }
}
