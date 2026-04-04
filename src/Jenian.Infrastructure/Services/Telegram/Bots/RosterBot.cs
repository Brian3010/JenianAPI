using Jenian.Application.Abstractions.AI;
using Jenian.Application.Abstractions.BackgroundJobs;
using Jenian.Application.Features.Telegram.Dtos;
using Jenian.Infrastructure.BackgroundJobs.JobPayloads;
using System.Collections.Concurrent;
using static Jenian.Infrastructure.Services.Telegram.Bots.TelegramFileHandler;


namespace Jenian.Infrastructure.Services.Telegram.Bots

{
  public class RosterBot
  {
    private readonly ILogger<RosterBot> _logger;
    private readonly IConfiguration _configuration;
    private readonly IParserService _parserService;
    private readonly IBackgroundJobQueue<ShiftExtractionJob> _jobQueue;
    private readonly StateStore _stateStore;
    private readonly IHttpClientFactory _clientFactory;

    private string BotToken => _configuration["Telegram:BotToken"] ?? string.Empty;

    private ConcurrentDictionary<long, TaskCompletionSource<TelegramMessage>> _photoWaiters = new();

    private string ApiBase => $"https://api.telegram.org/bot{BotToken}";
    private string FileBase => $"https://api.telegram.org/file/bot{BotToken}";



    public RosterBot(ILogger<RosterBot> logger, IConfiguration configuration, IParserService parserService, IBackgroundJobQueue<ShiftExtractionJob> jobQueue, StateStore stateStore, IHttpClientFactory clientFactory) {
      _logger = logger;
      _configuration = configuration;
      _parserService = parserService;
      _jobQueue = jobQueue;
      _stateStore = stateStore;
      _clientFactory = clientFactory;
    }

    // PUBLIC: start the wait flow (call this on /r)
    public void StartRosterWait(long chatId, CancellationToken ct) {
      _ = StartPhotoFlowAsync(chatId, ct); // fire-and-forget the long flow
    }

    public void CancelIfExist(long chatId, CancellationToken ct) {
      if (_stateStore.Items.TryGetValue(chatId, out var existing)) {
        _logger.LogInformation("Canceling task: {0} ", existing);
        existing.TrySetCanceled();
        _stateStore.Items.TryRemove(new KeyValuePair<long, TaskCompletionSource<TelegramMessage>>(chatId, existing));
      }
    }


    public void TryCompleteWaitWithMessage(TelegramMessage msg) {
      if (msg?.Chat == null) return;

      if (_stateStore.Items.TryGetValue(msg.Chat.Id, out var tcs)) {
        // Accept Photo OR Document (optional)
        bool hasPhoto = msg.Photo?.Any() == true;
        bool hasImageDoc = msg.Document is { MimeType: not null } d &&
                           d.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        if (hasPhoto || hasImageDoc) {
          if (tcs.TrySetResult(msg)) {
            _stateStore.Items.TryRemove(new KeyValuePair<long, TaskCompletionSource<TelegramMessage>>(msg.Chat.Id, tcs));
          }
        } else {
          // Gentle nudge if they send text while we're waiting
          _ = SafeSendMessageAsync(msg.Chat.Id, "Please send a photo of the roster (image).");
        }
      }
    }

    public async Task HandleMediaAsync(TelegramMessage message, long chatId, CancellationToken ct = default) {


      var bestFileId = PickBestFileId(message);
      if (string.IsNullOrWhiteSpace(bestFileId)) {
        await SafeSendMessageAsync(chatId, "I couldn't find a valid photo/document in your message.", ct);
        return;
      }

      // Step 1: Resolve file path via getFile
      var downloadUrl = await GetDownloadUrlAsync(bestFileId, ct);

      // Step 2: Download to memory
      var fileByte = await DownloadToMemoryByteAsync(downloadUrl, ct);

      // Step 4: Parse with AI (Azure Vision service you wired up)
      var ocrText = "";
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
      cts.CancelAfter(TimeSpan.FromSeconds(180)); // keep webhook snappy; Telegram retries on long timeouts
      try {
        // Prepocess the photo for clearer text
        var cleanedPhoto = AI.OcrPreprocess.PhotoCleanUp(fileByte);
        await SafeSendMessageAsync(chatId, "The photo is processing...", ct);
        ocrText = await _parserService.ExtractTextFromPhotoAsync(cleanedPhoto, cts.Token);
      } catch (TaskCanceledException) {
        _logger.LogWarning("Parsing timed out.");
        await SafeSendMessageAsync(chatId, "⏱️ Parsing took too long. Please try again with a clearer photo.", ct);
        return;
      } catch (Exception ex) {
        _logger.LogError(ex, "Parsing failed.");
        await SafeSendMessageAsync(chatId, "⚠️ I couldn't parse that image. Try a clearer/cropped photo of the roster.", ct);
        return;
      }

      // Step 5: (Future) Save parsed shift, reply with summary, etc.
      await SafeSendMessageAsync(chatId, "✅ Photo processed. I'll add the shift details shortly.", ct);

      // This name should be obtained by user specification at the frontend
      // -> save the name that they want to extract in the database. 
      //TODO: ask to save the name to extract in the database
      var staffName = "Brian Nguyen";

      await _jobQueue.EnqueueAsync(
      new ShiftExtractionJob(chatId, ocrText, staffName),
      ct);


    }

    public async Task StartPhotoFlowAsync(long chatId, CancellationToken ct = default) {


      // Clear previous waiter if exist
      if (_stateStore.Items.TryGetValue(chatId, out var existing)) {
        existing.TrySetCanceled();
        _stateStore.Items.TryRemove(new KeyValuePair<long, TaskCompletionSource<TelegramMessage>>(chatId, existing));
      }

      // if no preivous waiter then, create new waiter
      var waiter = new TaskCompletionSource<TelegramMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
      _stateStore.Items[chatId] = waiter;

      //(Optional)add a timeout so the bot doesn't wait forever
      var timeoutTask = Task.Delay(TimeSpan.FromMinutes(1), ct); //TODO: 
      var completed = await Task.WhenAny(waiter.Task, timeoutTask);

      if (completed != waiter.Task) {
        // Timed out
        await SafeSendMessageAsync(chatId, "Timed out waiting for a photo. Send /r to try again.", ct);
        _stateStore.Items.TryRemove(new KeyValuePair<long, TaskCompletionSource<TelegramMessage>>(chatId, waiter));
        return;
      }


      _logger.LogInformation("Before calling watier.Task");
      // THIS AWAITS UNTIL SOMEONE CALLS TrySetResult(...)
      var photoMsg = await waiter.Task;
      _logger.LogInformation("After calling watier.Task");
      await HandleMediaAsync(photoMsg, chatId, ct);

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
        using var client = _clientFactory.CreateClient();
        using var resp = await client.PostAsJsonAsync(url, payload, ct);
        if (!resp.IsSuccessStatusCode) {
          var body = await resp.Content.ReadAsStringAsync(ct);
          _logger.LogWarning("sendMessage failed: {Code} {Body}", resp.StatusCode, body);
        }
      } catch (OperationCanceledException oce) {
        _logger.LogWarning(oce, "sendMessage canceled/timed out (chat {ChatId})", chatId);
      } catch (Exception ex) {
        _logger.LogError(ex, "sendMessage exception (chat {ChatId})", chatId);
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
        using var client = _clientFactory.CreateClient();

        var res = await client.GetFromJsonAsync<TelegramFileResponse>(url, ct);
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
        using var client = _clientFactory.CreateClient();

        var bytes = await client.GetByteArrayAsync(url, ct);
        return bytes;
      } catch (Exception ex) {
        _logger.LogError(ex, "Failed to download media from {Url}", url);
        throw;
      }
    }

    public void CancelTask(long chatId) {
      if (_stateStore.Items.TryGetValue(chatId, out var existing)) {
        existing.TrySetCanceled();
        _stateStore.Items.TryRemove(new KeyValuePair<long, TaskCompletionSource<TelegramMessage>>(chatId, existing));
      }


    }



  }
}
