namespace Jenian.Infrastructure.Identity
{
  public class RefreshToken
  {
    public required Guid Id { get; set; }

    public required string Token { get; set; }

    public Guid DeviceId { get; set; }          // e.g., "Chrome on Macbook"

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiredAt { get; set; }

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public required string UserId { get; set; }

    // Navitation property
    public ApplicationUser User { get; set; } = null!;

  }
}
