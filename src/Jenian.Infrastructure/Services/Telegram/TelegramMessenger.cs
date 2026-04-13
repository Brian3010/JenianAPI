using Jenian.Application.Abstractions.Messaging;
using Jenian.Application.Features.Telegram.Dtos;

namespace Jenian.Infrastructure.Services.Telegram
{
  public class TelegramMessenger : ITelegramMessenger
  {
    private readonly ILogger<TelegramMessenger> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _clientFactory;

    private string BotToken => _configuration["Telegram:BotToken"] ?? string.Empty;
    private string ApiBase => $"https://api.telegram.org/bot{BotToken}";

    public TelegramMessenger(ILogger<TelegramMessenger> logger, IConfiguration configuration, IHttpClientFactory clientFactory) {
      _logger = logger;
      _configuration = configuration;
      _clientFactory = clientFactory;
    }

    /// <summary>
    /// Sends a message to Telegram; never throws to caller.
    /// </summary>
    public async Task SendMessageAsync(long chatId, string text, CancellationToken ct = default) {
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



  }
}
