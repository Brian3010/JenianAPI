using System.Text.Json.Serialization;

namespace Jenian.Infrastructure.Services.Telegram.Bots
{
  public class TelegramFileHandler
  {
    public class TelegramFileResponse
    {
      public bool Ok { get; set; }
      public TelegramFile Result { get; set; } = null!;
    }

    public class TelegramFile
    {
      [JsonPropertyName("file_path")]
      public string FilePath { get; set; } = null!;
    }

  }
}
