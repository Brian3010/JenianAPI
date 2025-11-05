using JenianAPI.Data;
using JenianAPI.Dtos.TelegramDtos;
using JenianAPI.Services.Interfaces;
using JenianAPI.TelegramBot;
using JenianAPI.Workers;
using JenianAPI.Workers.JobPayloads;
using Microsoft.EntityFrameworkCore;

namespace JenianAPI.Services
{
  /// <summary>
  /// Responsibilities:
  /// - Verify /start <token> and link a Telegram user to a Jenian account
  /// - Authorize subsequent messages (must come from a linked TelegramUserId)
  /// - Fetch photos/docs, compress to JPEG, and dispatch to the parser (Azure Vision, etc.)
  ///
  /// Key fixes vs. your version:
  /// 1) Defensive parsing for "/start" to avoid IndexOutOfRange on split.
  /// 2) Don’t crash on Telegram outages (no EnsureSuccessStatusCode without try/catch).
  /// 3) Stronger validation + logging in getFile/sendMessage flows.
  /// 4) Null-safe photo/document selection + prefer largest photo size.
  /// 5) Ensure MemoryStreams have Position reset (paired with ImageHelper fix).
  /// 6) Consistent UTC timestamps (if you add any here later).
  /// 7) Respect CancellationToken where sensible.
  /// </summary>
  public class TelegramService
  {
    private readonly JenianAuthDbContext _dbContext;
    private readonly ILogger<TelegramService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IParserService _parserService;
    private readonly IBackgroundJobQueue<ShiftExtractionJob> _jobQueue;
    private readonly RosterBot _rosterBot;

    public TelegramService(JenianAuthDbContext dbContext, ILogger<TelegramService> logger, HttpClient httpClient, IConfiguration configuration, IParserService parserService, IBackgroundJobQueue<ShiftExtractionJob> jobQueue, RosterBot rosterBot) {
      _dbContext = dbContext;
      _logger = logger;
      _httpClient = httpClient;
      _configuration = configuration;
      _parserService = parserService;
      _jobQueue = jobQueue;
      _rosterBot = rosterBot;
    }

    private string BotToken => _configuration["Telegram:BotToken"] ?? string.Empty;
    private string ApiBase => $"https://api.telegram.org/bot{BotToken}";
    private string FileBase => $"https://api.telegram.org/file/bot{BotToken}";

    // Runs on webhook POST /api/telegram/webhook
    public async Task HandleUpdateAsync(TelegramUpdate update, CancellationToken ct) {
      var msg = update.Message;
      if (msg == null) {
        _logger.LogInformation("Telegram webhook: update has no message.");
        return;
      }

      var chatId = msg.Chat?.Id ?? msg.From?.Id ?? 0;
      if (chatId == 0) {
        _logger.LogInformation("Telegram webhook: cannot resolve chatId.");
        return;
      }

      // Short-circuit: empty payload (no text, no photo, no document)
      var hasAnyContent = !string.IsNullOrWhiteSpace(msg.Text) || msg.Photo != null || msg.Document != null;
      if (!hasAnyContent) {
        _logger.LogInformation("Telegram webhook: empty message (no text/photo/document).");
        await SafeSendMessageAsync(chatId, "I received your message but couldn’t find any text or photo to process.", ct);
        return;
      }

      _logger.LogInformation("Telegram webhook from @{Username} (Id:{Id}). Text:'{Text}' Photo:{HasPhoto} Doc:{HasDoc}",
        msg.From?.Username, msg.From?.Id, msg.Text, msg.Photo != null, msg.Document != null);

      // 1) Handle linking flow: /start <linkToken>
      if (!string.IsNullOrWhiteSpace(msg.Text) && msg.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase)) {
        var maybeToken = TryExtractStartToken(msg.Text);
        if (string.IsNullOrEmpty(maybeToken)) {
          await SafeSendMessageAsync(chatId, "Please open the link from the Jenian app so I can connect your Telegram.", ct);
          return;
        }

        var linkToken = maybeToken!;
        var user = await _dbContext
          .Users
          .FirstOrDefaultAsync(u => u.TelegramLinkToken == linkToken, ct);

        if (user == null) {
          _logger.LogInformation("Telegram link: invalid or expired token '{Token}'", linkToken);
          await SafeSendMessageAsync(chatId, "Invalid or expired token. Please try connecting again from the Jenian app.", ct);
          return;
        }

        // Prevent one Telegram account linking to multiple Jenian users
        var telegramIdStr = (msg.From?.Id ?? 0).ToString();
        var alreadyLinked = await _dbContext
          .Users
          .AnyAsync(u => u.TelegramUserId == telegramIdStr && u.Id != user.Id, ct);

        if (alreadyLinked) {
          await SafeSendMessageAsync(chatId, "This Telegram account is already linked to another Jenian user.", ct);
          return;
        }

        user.TelegramUserId = telegramIdStr;
        user.TelegramLinkToken = null; // one-time token → invalidate
        await _dbContext.SaveChangesAsync(ct);

        await SafeSendMessageAsync(chatId, $"✅ Your Telegram is now linked to Jenian ({user.Email}).");
        return;
      }

      // 2) Gate all further interactions to linked users only
      var fromTelegramId = (msg.From?.Id ?? 0).ToString();
      var linkedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramUserId == fromTelegramId, ct);
      if (linkedUser == null) {
        _logger.LogInformation("Unauthorized Telegram user {TelegramId} tried to interact.", fromTelegramId);
        await SafeSendMessageAsync(chatId, "You're not authorized yet. Please connect Telegram from the Jenian app first.", ct);
        return;
      }

      //// 3) Acknowledge and start processing
      //await SafeSendMessageAsync(chatId, "📥 Got it — processing now…", ct);

      // 4) Route by content: photo/document → parse image; text → placeholder for commands
      try {

        bool hasPhoto = msg.Photo?.Any() == true;
        bool hasImageDoc = msg.Document is { MimeType: not null } d &&
                           d.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        if (hasPhoto || hasImageDoc) {
          _rosterBot.TryCompleteWaitWithMessage(msg);
        }
        if (string.IsNullOrWhiteSpace(msg.Text)) {
          //await SafeSendMessageAsync(chatId, "Message is empty");
          return;
        } else if (msg.Text.Contains("/r") || msg.Text.Contains("/roster")) {
          await SafeSendMessageAsync(chatId, "Please send me the roster", ct);
          _rosterBot.StartRosterWait(chatId, ct);   // fire-and-forget the flow
          //_ = StartPhotoFlowAsync(chatId);



        } else if (msg.Text.Contains("/d") || msg.Text!.Contains("/delivery")) {

          await HandleDeliveryReport(msg, chatId, ct);

        } else {
          await SafeSendMessageAsync(chatId, "Please enter following commands: \n\n" +
            "/d or /delivery - Summarise daily delivery report for Chemist Warehouse\n" +
            "/r or /roster - Extract shifts from a photo roster", ct);
        }





      } catch (Exception ex) {
        _logger.LogError(ex, "Error while processing Telegram message for user {UserId}", linkedUser.Id);
        await SafeSendMessageAsync(chatId, "⚠️ Something went wrong while processing your message. Please try again.", ct);
      }
    }

    // --- Helpers ---
    /// <summary>
    /// Parses "/start <token>" safely. Returns null if missing.
    /// </summary>
    private static string? TryExtractStartToken(string text) {
      // robust split: "/start" OR "/start   token"
      var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      return parts.Length >= 2 ? parts[1] : null;
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
    /*
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

        /// <summary>
        /// Downloads a URL into a fresh MemoryStream positioned at 0.
        /// </summary>
        private async Task<MemoryStream> DownloadToMemoryStreamAsync(string url, CancellationToken ct = default) {
          try {
            var bytes = await _httpClient.GetByteArrayAsync(url, ct);
            var ms = new MemoryStream(bytes);
            ms.Position = 0;
            return ms;
          } catch (Exception ex) {
            _logger.LogError(ex, "Failed to download media from {Url}", url);
            throw;
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

        
        */

    /// <summary>
    /// Picks the best available file_id from photo/document payloads.
    /// - For photos: Telegram sends an array of sizes; we take the last (largest).
    /// - For documents: use the document’s file_id.
    /// Returns null if none present.
    /// </summary>
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

    /*
    /// <summary>
    /// Handles photo/document ingestion: getFile → download → compress → parse.
    /// </summary>
    private async Task HandleMediaAsync(TelegramMessage message, long chatId, CancellationToken ct = default) {
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
        var cleanedPhoto = OcrPreprocess.PhotoCleanUp(fileByte);
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


    }*/

    private async Task HandleDeliveryReport(TelegramMessage message, long chatID, CancellationToken ct = default) {


      await SafeSendMessageAsync(chatID, "kitayamachu", ct);
    }
















  }
}
