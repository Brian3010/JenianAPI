namespace JenianAPI.Dtos.TelegramDtos
{

  /* Whenever someone sends a message to your bot, Telegram sends a JSON POST to your webhook like this:
   * {
   *  "update_id": 123456,
   *  "message": {
   *    "message_id": 321,
   *    "from": {
   *      "id": 12345678,
   *      "username": "jenny",
   *      "first_name": "Jenny"},
   *    "chat": {
   *      "id": 12345678,
   *      "type": "private"},
   *      
   *    "text": "/start abc123"
   *  }
   * }
  */

  public class TelegramUpdate
  {
    public TelegramMessage? Message { get; set; }
  }

  public class TelegramMessage
  {
    public long MessageId { get; set; }
    public TelegramUser? From { get; set; }
    public TelegramChat? Chat { get; set; }
    public string? Text { get; set; }
  }

  public class TelegramUser
  {
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
  }

  public class TelegramChat
  {
    public long Id { get; set; }
    public string? Type { get; set; }
  }


}
