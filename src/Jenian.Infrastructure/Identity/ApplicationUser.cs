using Microsoft.AspNetCore.Identity;

namespace Jenian.Infrastructure.Identity
{
  public class ApplicationUser : IdentityUser
  {

    public List<RefreshToken> RefreshTokens { get; set; } = new();


    // Telegram user id, used to send messages to the user via Telegram bot
    public string? TelegramUserId { get; set; }

    // token to identify the user when they click on the link in the Telegram bot message, used to link the Telegram user to the application user
    public string? TelegramLinkToken { get; set; }

    // Demo users
    public bool IsDemoUser { get; set; }
    public DateTimeOffset? DemoCreatedAtUtc { get; set; }
    public DateTimeOffset? DemoExpiresAtUtc { get; set; }
    public DemoAccountStatus? DemoStatus { get; set; }
  }
}
