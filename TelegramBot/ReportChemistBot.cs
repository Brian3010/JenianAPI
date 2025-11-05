using JenianAPI.Dtos.TelegramDtos;
using JenianAPI.Services.Interfaces;

namespace JenianAPI.TelegramBot
{
  public class ReportChemistBot
  {
    private readonly ILogger<ReportChemistBot> _logger;
    private readonly ITelegramMessenger _telegramMessager;

    public ReportChemistBot(ILogger<ReportChemistBot> logger, ITelegramMessenger telegramMessager) {
      _logger = logger;
      _telegramMessager = telegramMessager;
    }

    public async Task HandleDeliveryReport(TelegramMessage message, long chatID, CancellationToken ct = default) {


      await _telegramMessager.SendMessageAsync(chatID, "processing...", ct);
    }

  }
}
