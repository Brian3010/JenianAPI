using Jenian.Application.Features.Telegram.Dtos;

namespace Jenian.Application.Abstractions.Messaging
{
  public interface ITelegramMessenger
  {
    Task SendMessageAsync(long chatId, string text, CancellationToken ct = default);

  }
}
