using Jenian.Application.Features.Telegram.Dtos;

namespace Jenian.Application.Abstractions.Messaging
{
  public interface IReportChemistBot
  {
    Task HandleDeliveryReport(TelegramMessage message, long chatID, CancellationToken ct = default);
  }
}
