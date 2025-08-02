using JenianAPI.Dtos.TelegramDtos;
using JenianAPI.Services.Interfaces;
using static JenianAPI.Dtos.TelegramDtos.TelegramFileHandler;
using static JenianAPI.Services.Interfaces.IParserService;

namespace JenianAPI.Services
{
  public class TelegramParserService : IParserService
  {
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramParserService> _logger;
    private readonly IConfiguration _configuration;

    public TelegramParserService(HttpClient httpClient, ILogger<TelegramParserService> logger, IConfiguration configuration) {
      _httpClient = httpClient;
      _logger = logger;
      _configuration = configuration;
    }

    private async Task<string> GetPhotoBase64Async(string downloadFileUrl) {
      var fileStream = await _httpClient.GetStreamAsync(downloadFileUrl);

      // 3. Convert stream to Base64
      using var ms = new MemoryStream();
      await fileStream.CopyToAsync(ms);
      var fileBytes = ms.ToArray();
      var base64Image = Convert.ToBase64String(fileBytes);

      return base64Image;
    }
    public async Task<ParseResult> ParseMessageAsync(List<TelegramPhoto>? photo, TelegramDocument? document, string? text, string? caption, long userId) {

      // TODO: implementing parser for photo, document, and text
      /**
       * Get the photo file_id from Telegram
       * Get the file_path using Telegram’s getFile API
       * Download the photo content (as bytes/stream)
       * Send it to OpenAI Vision API (GPT-4-Vision) for parsing
       * Process the response (shift info)
       * Save & reply to user
       */

      if (photo != null) {
        var photoFileId = photo.Last().FileId; // Telegram sends smallest to largest 
        var fileUrl = $"https://api.telegram.org/bot{_configuration["Telegram:BotToken"]}/getFile?file_id={photoFileId}";

        var res = await _httpClient.GetFromJsonAsync<TelegramFileResponse>(fileUrl);

        if (res == null || !res.Ok)
          return new ParseResult { Success = false, Message = "Failed to get file path from Telegram" };

        var downloadFilePath = $"https://api.telegram.org/file/bot{_configuration["Telegram:BotToken"]}/{res.Result.FilePath}";
        _logger.LogInformation($"downloadUrl: {downloadFilePath} | {fileUrl}");
        //TODO: Implement GetPhotoBase64Async method
        var base64Image = await GetPhotoBase64Async(downloadFilePath);

      }


      return new ParseResult {
        Success = true,
        FileDownloadUrl = downloadFilePath,
        ParsedShiftText = caption, // You can parse caption text further with AI
        Message = "Photo processed successfully"
      };

    }
  }
}
