namespace Jenian.API.Contracts.Auth
{
  public class LoginResponse
  {
    public required string Message { get; set; }
    public required string AccessToken { get; set; }
    public required UserDto User { get; set; }
  }
}
