using JenianAPI.Dtos.TelegramDtos;

namespace JenianAPI.Services.Interfaces
{
  public interface IParserService
  {
    public class ParseResult
    {
      public bool Success { get; set; }
      public string? Message { get; set; }  // For logs or errors
      public string? FileDownloadUrl { get; set; } // Download link from Telegram
      public string? ParsedShiftText { get; set; } // (Optional) AI extracted text
    }


    Task<ParseResult> ParseMessageAsync(List<TelegramPhoto>? photo, TelegramDocument? document, string? text, string? caption, long userId);



  }
}
