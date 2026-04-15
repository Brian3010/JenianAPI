using Jenian.Application.Abstractions.AI;
using Jenian.Application.Abstractions.BackgroundJobs;
using Jenian.Application.Abstractions.Messaging;
using Jenian.Application.Features.Telegram.Dtos;
using Jenian.Infrastructure.BackgroundJobs.JobPayloads;
using static Jenian.Infrastructure.Services.Telegram.Bots.TelegramFileHandler;


namespace Jenian.Infrastructure.Services.Telegram.Bots

{
  public class TableRosterExtractor : IRosterExtractor
  {
    private readonly ILogger<TableRosterExtractor> _logger;
    private readonly IConfiguration _configuration;
    private readonly IParserService _parserService;
    private readonly IBackgroundJobQueue<ShiftExtractionJob> _jobQueue;
    private readonly StateStore _stateStore;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ITelegramMessenger _telegramMessenger;

    private string BotToken => _configuration["Telegram:BotToken"] ?? string.Empty;

    private string ApiBase => $"https://api.telegram.org/bot{BotToken}";
    private string FileBase => $"https://api.telegram.org/file/bot{BotToken}";



    public TableRosterExtractor(ILogger<TableRosterExtractor> logger,
      IConfiguration configuration,
      IParserService parserService,
      IBackgroundJobQueue<ShiftExtractionJob> jobQueue,
      StateStore stateStore,
      IHttpClientFactory clientFactory,
      ITelegramMessenger telegramMessenger
      ) {
      _logger = logger;
      _configuration = configuration;
      _parserService = parserService;
      _jobQueue = jobQueue;
      _stateStore = stateStore;
      _clientFactory = clientFactory;
      _telegramMessenger = telegramMessenger;
    }



    public async Task HandleMediaAsync(string staffName, TelegramMessage message, long chatId, CancellationToken ct = default) {

      // step 1: pick the best fileId from the message (photo array's largest size or document)
      var bestFileId = PickBestFileId(message);
      if (string.IsNullOrWhiteSpace(bestFileId)) {
        await _telegramMessenger.SendMessageAsync(chatId, "I couldn't find a valid photo/document in your message.", ct);
        return;
      }

      // Step 2: Resolve file path via getFile
      var downloadUrl = await GetDownloadUrlAsync(bestFileId, ct);

      // Step 3: Download to memory
      var streamFile = await DownloadToStreamAsync(downloadUrl, ct);


      // Step 4: Parse with AI (Azure Vision service you wired up)
      var ocrText = "";
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
      cts.CancelAfter(TimeSpan.FromSeconds(180)); // keep webhook snappy; Telegram retries on long timeouts
      try {
        await _telegramMessenger.SendMessageAsync(chatId, "🔄 Photo received, processing...", ct);
        ocrText = await _parserService.ExtractTextFromRosterPhotoStreamAsync(streamFile, cts.Token);
      } catch (TaskCanceledException) {
        _logger.LogWarning("Parsing timed out.");
        await _telegramMessenger.SendMessageAsync(chatId, "⏱️ Parsing took too long. Please try again with a clearer photo.", ct);
        return;
      } catch (Exception ex) {
        _logger.LogError(ex, "Parsing failed.");
        await _telegramMessenger.SendMessageAsync(chatId, "⚠️ I couldn't parse that image. Try a clearer/cropped photo of the roster.", ct);
        return;
      }


      // Step 5: (Future) Save parsed shift, reply with summary, etc.
      //await _telegramMessenger.SendMessageAsync(chatId, "⏳ Hang on, almost there...", ct);

      await _jobQueue.EnqueueAsync(
      new ShiftExtractionJob(chatId, ocrText, staffName),
      ct);


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


    private async Task<Stream> DownloadToStreamAsync(string url, CancellationToken ct = default) {
      try {
        using var client = _clientFactory.CreateClient();

        //var bytes = await client.GetByteArrayAsync(url, ct);
        var stream = await client.GetStreamAsync(url, ct);

        return stream;
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
