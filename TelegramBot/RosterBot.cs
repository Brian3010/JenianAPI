using JenianAPI.Dtos.TelegramDtos;
using JenianAPI.Services.Interfaces;
using JenianAPI.Workers;
using JenianAPI.Workers.JobPayloads;
using static JenianAPI.Dtos.TelegramDtos.TelegramFileHandler;


namespace JenianAPI.TelegramBot

{
  public class RosterBot
  {
    private readonly ILogger<RosterBot> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IParserService _parserService;
    private readonly IBackgroundJobQueue<ShiftExtractionJob> _jobQueue;

    private string BotToken => _configuration["Telegram:BotToken"] ?? string.Empty;

    private string ApiBase => $"https://api.telegram.org/bot{BotToken}";
    private string FileBase => $"https://api.telegram.org/file/bot{BotToken}";



    public RosterBot(ILogger<RosterBot> logger, HttpClient httpClient, IConfiguration configuration, IParserService parserService, IBackgroundJobQueue<ShiftExtractionJob> jobQueue) {
      _logger = logger;
      _httpClient = httpClient;
      _configuration = configuration;
      _parserService = parserService;
      _jobQueue = jobQueue;
    }


    /// <summary>
    /// Sends a message to Telegram; never throws to caller.
    /// </summary>
    public async Task SafeSendMessageAsync(long chatId, string text, CancellationToken ct = default) {
      var url = $"{ApiBase}/sendMessage";
      var payload = new Dictionary<string, object> {
        ["chat_id"] = chatId,
        ["text"] = text
      };

      try {
        using var resp = await _httpClient.PostAsJsonAsync(url, payload, ct);
        if (!resp.IsSuccessStatusCode) {
          var body = await resp.Content.ReadAsStringAsync(ct);
          _logger.LogWarning("sendMessage failed: {Code} {Body}", resp.StatusCode, body);
        }
      } catch (Exception ex) {
        _logger.LogError(ex, "sendMessage exception");
      }
    }
    private static string? PickBestFileId(TelegramMessage msg) {
      if (msg.Photo is { Count: > 0 }) {
        // Photo array is sized smallest→largest
        return msg.Photo.Last().FileId;
      }

      if (msg.Document != null && !string.IsNullOrWhiteSpace(msg.Document.FileId)) {
        return msg.Document.FileId;
      }

      return null;
    }

    /// <summary>
    /// Calls getFile and returns a public download URL for the file.
    /// </summary>  
    private async Task<string> GetDownloadUrlAsync(string fileId, CancellationToken ct = default) {
      var url = $"{ApiBase}/getFile?file_id={Uri.EscapeDataString(fileId)}";
      try {
        var res = await _httpClient.GetFromJsonAsync<TelegramFileResponse>(url, ct);
        if (res == null || !res.Ok || res.Result == null || string.IsNullOrWhiteSpace(res.Result.FilePath))
          throw new InvalidOperationException($"getFile failed or empty file path for fileId: {fileId}");

        return $"{FileBase}/{res.Result.FilePath}";
      } catch (Exception ex) {
        _logger.LogError(ex, "getFile error for {FileId}", fileId);
        throw; // let caller handle (we catch upstream)
      }
    }


    private async Task<byte[]> DownloadToMemoryByteAsync(string url, CancellationToken ct = default) {
      try {
        var bytes = await _httpClient.GetByteArrayAsync(url, ct);
        return bytes;
      } catch (Exception ex) {
        _logger.LogError(ex, "Failed to download media from {Url}", url);
        throw;
      }
    }

    public async Task HandleMediaAsync(TelegramMessage message, long chatId, CancellationToken ct = default) {
      var bestFileId = PickBestFileId(message);
      if (string.IsNullOrWhiteSpace(bestFileId)) {
        await SafeSendMessageAsync(chatId, "I couldn’t find a valid photo/document in your message.", ct);
        return;
      }

      // Step 1: Resolve file path via getFile
      var downloadUrl = await GetDownloadUrlAsync(bestFileId, ct);

      // Step 2: Download to memory
      var fileByte = await DownloadToMemoryByteAsync(downloadUrl, ct);

      // Step 4: Parse with AI (Azure Vision service you wired up)
      var ocrText = "";
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
      cts.CancelAfter(TimeSpan.FromSeconds(20)); // keep webhook snappy; Telegram retries on long timeouts
      try {
        // Prepocess the photo for clearer text
        var cleanedPhoto = Services.OcrPreprocess.PhotoCleanUp(fileByte);
        //// pick a folder you can find (Desktop/roster-debug)
        //var outDir = Path.Combine(
        //    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        //    "roster-debug");
        //Directory.CreateDirectory(outDir);

        //var outPath = Path.Combine(outDir, $"roster-straight-{DateTime.Now:yyyyMMdd_HHmmss}.png");
        //await File.WriteAllBytesAsync(outPath, cleanedPhoto);

        //Console.WriteLine($"Saved → {outPath}");
        ocrText = await _parserService.ExtractTextFromPhotoAsync(cleanedPhoto, cts.Token);
      } catch (TaskCanceledException) {
        _logger.LogWarning("Parsing timed out.");
        await SafeSendMessageAsync(chatId, "⏱️ Parsing took too long. Please try again with a clearer photo.", ct);
        return;
      } catch (Exception ex) {
        _logger.LogError(ex, "Parsing failed.");
        await SafeSendMessageAsync(chatId, "⚠️ I couldn’t parse that image. Try a clearer/cropped photo of the roster.", ct);
        return;
      }

      // Step 5: (Future) Save parsed shift, reply with summary, etc.
      await SafeSendMessageAsync(chatId, "✅ Photo processed. I’ll add the shift details shortly.", ct);

      // This name should be obtained by user specification at the frontend
      // -> save the name that they want to extract in the database. 
      //TODO: ask to save the name to extract in the database
      var staffName = "Brian Nguyen";

      //var answer = await _parserService.ExtractShiftAsync(ocrText, staffName, cts.Token);
      //await SafeSendMessageAsync(chatId, $"✅ Here is the roster: \n {answer}");

      await _jobQueue.EnqueueAsync(
      new ShiftExtractionJob(chatId, ocrText, staffName),
      ct);


    }



  }
}
