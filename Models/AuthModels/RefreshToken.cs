namespace JenianAPI.Models.AuthModels
{
  public class RefreshToken
  {
    public required Guid Id { get; set; }

    public required string Token { get; set; }

    public string DeviceName { get; set; }          // e.g., "Chrome on Macbook"
    public string DeviceIpAddress { get; set; }     // e.g., "192.168.1.20"

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiredAt { get; set; }

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public required string UserId { get; set; }

    // Navitation property
    public ApplicationUser User { get; set; }

  }
}
