using JenianAPI.Services.Interfaces;

namespace JenianAPI.Services
{
  public class TelegramMessenger : ITelegramMessenger
  {
    private readonly TelegramService _inner;

    public TelegramMessenger(TelegramService inner) {
      _inner = inner;
    }

    public Task SafeSendMessageAsync(long chatId, string text, CancellationToken ct = default) =>
      _inner.SafeSendMessageAsync(chatId, text, ct);

  }
}
