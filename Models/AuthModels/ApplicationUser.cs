using Microsoft.AspNetCore.Identity;

namespace JenianAPI.Models.AuthModels
{
  public class ApplicationUser : IdentityUser
  {

    public List<RefreshToken> RefreshTokens { get; set; } = new();


    /**
     * TelegramLinkToken: Temporary one-time link token
     * How it works:
     * You generate a random token (e.g. f234abcd...)
     * You send it to the frontend as part of the Connect Telegram flow
     * The frontend opens:
     * https://t.me/jenian_assistant_bot?start=f234abcd...
     * When the bot receives /start f234abcd, it looks up the token in your DB
     * if found → that tells you which Jenian user sent the message
     * Store the Telegram ID (message.from.id) in TelegramUserId
     * Remove the TelegramLinkToken (invalidate it)
     * 
     * 
     * TelegramUserId: Long-term account association
     * Why you need it:
     * Once the bot receives a message from a user, you only get:
     * "from": {
          "id": 12345678,
           "username": "jen"
        }

     * You need a way to know:
     * “Ah, 12345678 belongs to user@example.com in Jenian!”
     * That’s what TelegramUserId is for. It becomes your permanent lookup key.
     *
     */
    public string? TelegramUserId { get; set; }

    public string? TelegramLinkToken { get; set; } // user to identify telegram user
  }
}
