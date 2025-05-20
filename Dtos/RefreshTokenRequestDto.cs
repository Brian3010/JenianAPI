namespace JenianAPI.Dtos
{
  public class RefreshTokenRequestDto
  {
    public required string UserId { get; set; }

    public required string DeviceName { get; set; }

  }
}
