using System.Text.Json.Serialization;

namespace Jenian.Application.Features.Telegram.Dtos
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
    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; set; }
  }

  public class TelegramMessage
  {
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("from")]
    public TelegramUser? From { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChat? Chat { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("photo")]
    public List<TelegramPhoto>? Photo { get; set; }


    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    [JsonPropertyName("document")]
    public TelegramDocument? Document { get; set; }
  }
  public class TelegramPhoto
  {
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = null!;

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } = null!;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("file_size")]
    public int FileSize { get; set; }
  }

  public class TelegramDocument
  {
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = null!;

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } = null!;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = null!;

    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = null!;

    [JsonPropertyName("file_size")]
    public int FileSize { get; set; }
  }


  public class TelegramUser
  {
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
  }

  public class TelegramChat
  {
    [JsonPropertyName("id")]
    public long Id { get; set; }


    [JsonPropertyName("type")]
    public string? Type { get; set; }
  }


}
