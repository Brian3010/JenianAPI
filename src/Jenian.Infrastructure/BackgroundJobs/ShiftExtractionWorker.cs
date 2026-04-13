using Jenian.Application.Abstractions.AI;
using Jenian.Application.Abstractions.BackgroundJobs;
using Jenian.Application.Abstractions.Messaging;
using Jenian.Infrastructure.BackgroundJobs.JobPayloads;

namespace Jenian.Infrastructure.BackgroundJobs
{
  public class ShiftExtractionWorker : BackgroundService
  {
    private readonly IBackgroundJobQueue<ShiftExtractionJob> _backgroundJobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShiftExtractionWorker> _logger;

    public ShiftExtractionWorker(IBackgroundJobQueue<ShiftExtractionJob> backgroundJobQueue, IServiceScopeFactory scopeFactory, ILogger<ShiftExtractionWorker> logger) {
      _backgroundJobQueue = backgroundJobQueue;
      _scopeFactory = scopeFactory;
      _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
      _logger.LogInformation("ShiftExtractionWorker started");

      while (!stoppingToken.IsCancellationRequested) {
        var job = await _backgroundJobQueue.DequeueAsync(stoppingToken);
        // Create a scope for each job (or each batch/iteration)
        using var scope = _scopeFactory.CreateScope();
        var parser = scope.ServiceProvider.GetRequiredService<IParserService>();
        var telegramMessenger = scope.ServiceProvider.GetRequiredService<ITelegramMessenger>();
        try {
          await telegramMessenger.SendMessageAsync(job.ChatId, "🐢 Brian is reading...", stoppingToken);
          var answer = await parser.ExtractShiftAsync(job.OcrText, job.StaffName, stoppingToken);
          await telegramMessenger.SendMessageAsync(job.ChatId, $"✅ Here is the roster: \n {answer}");
        } catch (OperationCanceledException) {

        } catch (Exception e) {
          _logger.LogError(e, "Failed processing ShiftExtractionJob for ChatId {ChatId}", job.ChatId);
          await telegramMessenger.SendMessageAsync(job.ChatId,
                   "⚠️ I couldn't finish extracting your roster. Please try again.", stoppingToken);
        }

      }

    }
  }
}
