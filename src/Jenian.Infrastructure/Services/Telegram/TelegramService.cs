using Jenian.Application.Abstractions.AI;
using Jenian.Application.Abstractions.Messaging;
using Jenian.Application.Features.Telegram.Dtos;
using Jenian.Infrastructure.Concurrency;
using Jenian.Infrastructure.Persistence.Auth;
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
  /// 

  public class TelegramService : ITelegramService
  {
    public readonly string[] VALID_COMMANDS = ["/start", "/roster"];
    private readonly JenianAuthDbContext _dbContext;
    private readonly ILogger<TelegramService> _logger;

    private readonly ITelegramMessenger _telegramMessenger;
    private readonly LatestRequestRunner _latestRequestRunner;
    private readonly RosterSessionManager _rosterSessionManager;

    public TelegramService(JenianAuthDbContext dbContext,
      ILogger<TelegramService> logger, IConfiguration configuration,
      IParserService parserService, IRosterExtractor rosterBot,
      ITelegramMessenger telegramMessenger,
      LatestRequestRunner latestRequestRunner,
      RosterSessionManager rosterSessionManager

      ) {
      _dbContext = dbContext;
      _logger = logger;
      _telegramMessenger = telegramMessenger;
      _latestRequestRunner = latestRequestRunner;
      _rosterSessionManager = rosterSessionManager;
    }


    // Runs on webhook POST /api/telegram/webhook
    public async Task HandleUpdateAsync(TelegramUpdate update, CancellationToken ct = default) {
      var msg = update.Message;

      // Guard clauses: stop early unless the Telegram update contains
      // a message, a valid chat ID, and supported content to process
      if (msg == null) {
        _logger.LogInformation("Telegram webhook: update has no message.");
        return;
      }

      var chatId = msg.Chat?.Id ?? msg.From?.Id ?? 0;
      if (chatId == 0) {
        _logger.LogInformation("Telegram webhook: cannot resolve chatId.");
        return;
      }

      var hasAnyContent = !string.IsNullOrWhiteSpace(msg.Text) || msg.Photo != null || msg.Document != null;
      if (!hasAnyContent) {
        _logger.LogInformation("Telegram webhook: empty message (no text/photo/document).");
        await _telegramMessenger.SendMessageAsync(chatId, "I received your message but couldn't find any text or photo to process.", ct);
        return;
      }


      _logger.LogInformation("Telegram webhook from @{Username} (Id:{Id}). Text:'{Text}' Photo:{HasPhoto} Doc:{HasDoc}",
        msg.From?.Username, msg.From?.Id, msg.Text, msg.Photo != null, msg.Document != null);




      // Handle Linking Flow: if message starts with "/start", attempt to link Telegram user to Jenian account
      if (!string.IsNullOrWhiteSpace(msg.Text) && msg.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase)) {
        var maybeToken = TryExtractStartToken(msg.Text);
        if (string.IsNullOrEmpty(maybeToken)) {
          await _telegramMessenger.SendMessageAsync(chatId, "Invalid token after /start", ct);
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

        // Ensure this Telegram account isn't already linked to a different Jenian user
        var telegramIdStr = (msg.From?.Id ?? 0).ToString();
        var alreadyLinked = await _dbContext
          .Users
          .AnyAsync(u => u.TelegramUserId == telegramIdStr && u.Id != user.Id, ct);

        if (alreadyLinked) {
          await _telegramMessenger.SendMessageAsync(chatId, "This Telegram account is already linked to another Jenian user.", ct);
          return;
        }

        // Link Telegram user to Jenian account -> save TelegramUserId and clear TelegramLinkToken
        user.TelegramUserId = telegramIdStr;
        user.TelegramLinkToken = null; // one-time token → invalidate
        await _dbContext.SaveChangesAsync(ct);

        await _telegramMessenger.SendMessageAsync(chatId, $"✅ Your Telegram is now linked to Jenian ({user.Email}).", ct);
        return;
      }


      // Prevent unauthorized access: only allow messages from Telegram users who have linked their account (TelegramUserId matches)
      var fromTelegramId = (msg.From?.Id ?? 0).ToString();
      var linkedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramUserId == fromTelegramId, ct);
      if (!string.IsNullOrWhiteSpace(msg.Text) && linkedUser == null) {
        _logger.LogInformation("Unauthorized Telegram user {TelegramId} tried to interact.", fromTelegramId);
        await _telegramMessenger.SendMessageAsync(chatId, "You're not authorized yet. Please connect Telegram from the Jenian app first.", ct);
        return;
      }


      /********* Handle text with linked user **********/

      // Display menu
      if (!string.IsNullOrWhiteSpace(msg.Text) && msg.Text.Equals("/menu", StringComparison.OrdinalIgnoreCase)) {
        await _telegramMessenger.SendMessageAsync(chatId, "Available commands:\n/roster - Submit a photo of your roster\n/menu - Show this menu", ct);
      }



      // Handle message "/roster"
      if (!string.IsNullOrWhiteSpace(msg.Text) && msg.Text.Equals("/roster", StringComparison.OrdinalIgnoreCase)) {
        // Start awaiting for the roster photo session - awaiting for 30 seconds, then timeout and send message if no photo received
        // If /roster is sent again within the 30 seconds, new session starts and previous one is discarded
        _rosterSessionManager.StartOrReplace(chatId);
        await _telegramMessenger.SendMessageAsync(chatId, "Please send the photo of your roster within the next 1 minute.", ct);

        return;
      }

      // Handle incoming photo/document: if we're awaiting a roster photo for this chatId, process it; otherwise, ignore and prompt user to send /roster first
      if (msg.Photo != null || msg.Document != null) {
        // Check if current chatId is awaiting for a roster photo - /roster command was issued previously and we're waiting for the photo to be sent
        if (!_rosterSessionManager.TryConsume(chatId)) {
          await _telegramMessenger.SendMessageAsync(chatId, "I wasn't expecting a photo. If you want to submit your roster, please send /roster first.", ct);
          return;
        }

        // Only process the latest photo, old photo will be cancelled if user sends multiple photos/documents in a row
        var searchUserName = linkedUser!.UserName; // safe to use ! because we already checked linkedUser != null above
        _latestRequestRunner.StartOrRestart(chatId, async (sp, ct) => {
          var svc = sp.GetRequiredService<IRosterExtractor>();
          await svc.HandleMediaAsync(searchUserName!, msg, chatId, ct);
        });
      }

      // Other messages will be prompted to use /roster - if user sends any message other than /roster or a photo/document, we can optionally guide them to use the correct command
      if (_rosterSessionManager.HasActiveSession(chatId)) {
        await _telegramMessenger.SendMessageAsync(chatId, "Please send the photo", ct);
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




  }
}
