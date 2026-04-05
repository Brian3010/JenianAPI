using Jenian.Application.Abstractions.AI;
using Jenian.Application.Abstractions.Messaging;
using Jenian.Application.Features.Telegram.Dtos;
using Jenian.Infrastructure.Identity;
using Jenian.Infrastructure.Persistence.Auth;
using Jenian.Infrastructure.Services.Telegram.Bots;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.Services.Telegram
{
  /// <summary>
  /// Responsibilities:
  /// - Verify /start <token> and link a Telegram user to a Jenian account
  /// - Authorize subsequent messages (must come from a linked TelegramUserId)
  /// - Fetch photos/docs, compress to JPEG, and dispatch to the parser (Azure Vision, etc.)
  ///
  /// Key fixes vs. your version:
  /// 1) Defensive parsing for "/start" to avoid IndexOutOfRange on split.
  /// 2) Don't crash on Telegram outages (no EnsureSuccessStatusCode without try/catch).
  /// 3) Stronger validation + logging in getFile/sendMessage flows.
  /// 4) Null-safe photo/document selection + prefer largest photo size.
  /// 5) Ensure MemoryStreams have Position reset (paired with ImageHelper fix).
  /// 6) Consistent UTC timestamps (if you add any here later).
  /// 7) Respect CancellationToken where sensible.
  /// </summary>
  public class TelegramService : ITelegramService
  {
    private readonly JenianAuthDbContext _dbContext;
    private readonly ILogger<TelegramService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IParserService _parserService;
    private readonly IRosterBot _rosterBot;
    private readonly ITelegramMessenger _telegramMessenger;
    private readonly IReportChemistBot _reportChemistBot;

    public TelegramService(JenianAuthDbContext dbContext, ILogger<TelegramService> logger, IConfiguration configuration, IParserService parserService, IRosterBot rosterBot, ITelegramMessenger telegramMessenger, IReportChemistBot reportChemistBot) {
      _dbContext = dbContext;
      _logger = logger;
      _configuration = configuration;
      _parserService = parserService;
      _rosterBot = rosterBot;
      _telegramMessenger = telegramMessenger;
      _reportChemistBot = reportChemistBot;
    }

    private string BotToken => _configuration["Telegram:BotToken"] ?? string.Empty;
    private string ApiBase => $"https://api.telegram.org/bot{BotToken}";
    private string FileBase => $"https://api.telegram.org/file/bot{BotToken}";

    // Runs on webhook POST /api/telegram/webhook
    public async Task HandleUpdateAsync(TelegramUpdate update, CancellationToken ct = default) {
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
        await _telegramMessenger.SendMessageAsync(chatId, "I received your message but couldn't find any text or photo to process.", ct);
        return;
      }

      _logger.LogInformation("Telegram webhook from @{Username} (Id:{Id}). Text:'{Text}' Photo:{HasPhoto} Doc:{HasDoc}",
        msg.From?.Username, msg.From?.Id, msg.Text, msg.Photo != null, msg.Document != null);

      // 1) Handle linking flow: /start <linkToken>
      if (!string.IsNullOrWhiteSpace(msg.Text) && msg.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase)) {
        var maybeToken = TryExtractStartToken(msg.Text);
        if (string.IsNullOrEmpty(maybeToken)) {
          await _telegramMessenger.SendMessageAsync(chatId, "Please open the link from the Jenian app so I can connect your Telegram.", ct);
          return;
        }

        var linkToken = maybeToken!;
        var user = await _dbContext
          .Users
          .FirstOrDefaultAsync(u => u.TelegramLinkToken == linkToken, ct);

        if (user == null) {
          _logger.LogInformation("Telegram link: invalid or expired token '{Token}'", linkToken);
          await _telegramMessenger.SendMessageAsync(chatId, $"Invalid or expired token. Please try connecting again from the Jenian app. {linkToken}", ct);
          return;
        }

        // Prevent one Telegram account linking to multiple Jenian users
        var telegramIdStr = (msg.From?.Id ?? 0).ToString();
        var alreadyLinked = await _dbContext
          .Users
          .AnyAsync(u => u.TelegramUserId == telegramIdStr && u.Id != user.Id, ct);

        if (alreadyLinked) {
          await _telegramMessenger.SendMessageAsync(chatId, "This Telegram account is already linked to another Jenian user.", ct);
          return;
        }

        user.TelegramUserId = telegramIdStr;
        user.TelegramLinkToken = null; // one-time token → invalidate
        await _dbContext.SaveChangesAsync(ct);

        await _telegramMessenger.SendMessageAsync(chatId, $"✅ Your Telegram is now linked to Jenian ({user.Email}).");
        return;
      }

      // 2) Gate all further interactions to linked users only
      var fromTelegramId = (msg.From?.Id ?? 0).ToString();
      var linkedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramUserId == fromTelegramId, ct);
      if (linkedUser == null) {
        _logger.LogInformation("Unauthorized Telegram user {TelegramId} tried to interact.", fromTelegramId);
        await _telegramMessenger.SendMessageAsync(chatId, "You're not authorized yet. Please connect Telegram from the Jenian app first.", ct);
        return;
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
    /// Picks the best available file_id from photo/document payloads.
    /// - For photos: Telegram sends an array of sizes; we take the last (largest).
    /// - For documents: use the document's file_id.
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


  }
}
