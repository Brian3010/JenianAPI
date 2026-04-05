using Jenian.Application.Features.Telegram.Dtos;

namespace Jenian.Application.Abstractions.Messaging
{
  public interface IRosterBot
  {
    void StartRosterWait(long chatId, CancellationToken ct);
    void TryCompleteWaitWithMessage(TelegramMessage msg);
    Task HandleMediaAsync(TelegramMessage message, long chatId, CancellationToken ct = default);
    void CancelTask(long chatId);
  }
}
