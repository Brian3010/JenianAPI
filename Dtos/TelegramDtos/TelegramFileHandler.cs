using System.Text.Json.Serialization;

namespace JenianAPI.Dtos.TelegramDtos
{
  public class TelegramFileHandler
  {
    public class TelegramFileResponse
    {
      public bool Ok { get; set; }
      public TelegramFile Result { get; set; }
    }

    public class TelegramFile
    {
      [JsonPropertyName("file_path")]
      public string FilePath { get; set; }
    }

  }
}
