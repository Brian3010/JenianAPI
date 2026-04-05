using Jenian.Application.Features.Telegram.Dtos;

namespace Jenian.Application.Abstractions.Messaging
{
  public interface ITelegramService
  {
    Task HandleUpdateAsync(TelegramUpdate update, CancellationToken ct = default);
  }
}
