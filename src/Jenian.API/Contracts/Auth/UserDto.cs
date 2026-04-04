namespace Jenian.API.Contracts.Auth
{
  public class UserDto
  {
    public required string Id { get; set; }
    public string? UserName { get; set; }

    public string? Email { get; set; }
  }
}
