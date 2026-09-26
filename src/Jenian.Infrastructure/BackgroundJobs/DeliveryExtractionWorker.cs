using Jenian.Application.Abstractions.AI;
using Jenian.Application.Abstractions.BackgroundJobs;
using Jenian.Application.Abstractions.Messaging;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Abstractions.Storage;
using Jenian.Domain.Entities;
using Jenian.Infrastructure.BackgroundJobs.JobPayloads;
using Jenian.Infrastructure.Identity;
using Jenian.Infrastructure.Persistence.App;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Jenian.Infrastructure.BackgroundJobs
{
  public class DeliveryExtractionWorker : BackgroundService
  {
    private readonly IBackgroundJobQueue<DeliveryWorkerJob> _backgroundJobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveryExtractionWorker> _logger;

    public DeliveryExtractionWorker(IBackgroundJobQueue<DeliveryWorkerJob> backgroundJobQueue, IServiceScopeFactory scopeFactory,
      ILogger<DeliveryExtractionWorker> logger, IHttpClientFactory httpClientFactory) {
      _backgroundJobQueue = backgroundJobQueue;
      _scopeFactory = scopeFactory;
      _logger = logger;
    }


    private async Task<string> BuildOcrTextAsync(
    List<string> blobNames,
    IBlobStorageService blobStorageService,
    IParserService azureParser,
    CancellationToken cancellationToken) {
      if (blobNames == null || blobNames.Count == 0)
        throw new InvalidOperationException("No blob names were provided for OCR processing.");

      var allOcrText = new StringBuilder();

      foreach (var blobName in blobNames) {
        if (string.IsNullOrWhiteSpace(blobName))
          continue;

        // Get blob stream from storage and extract text using Azure OCR
        await using var stream = await blobStorageService.OpenReadAsync(blobName, cancellationToken);

        var ocrText = await azureParser.ExtractTextFromDeliveryPhotoStreamAsync(
            stream,
            cancellationToken
            );

        if (!string.IsNullOrWhiteSpace(ocrText))
          allOcrText.AppendLine(ocrText.Trim());
      }

      var finalOcrText = allOcrText.ToString().Trim();

      //if (string.IsNullOrWhiteSpace(finalOcrText))
      //  throw new InvalidOperationException("OCR completed but produced no text.");

      return finalOcrText ?? string.Empty;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
      _logger.LogInformation("Background worker for delivery extractor run");

      while (!stoppingToken.IsCancellationRequested) {
        var job = await _backgroundJobQueue.DequeueAsync(stoppingToken);
        _logger.LogInformation("Dequeued background Job: {@job}", job);
        await ProcessJobAsync(job, stoppingToken);
      }

    }

    private async Task ProcessJobAsync(DeliveryWorkerJob job, CancellationToken stoppingToken) {
      try {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var jenianDbContext = scope.ServiceProvider.GetRequiredService<JenianDbContext>();

        var bgJob = await jenianDbContext.DeliveryExtractionJobs
          .FirstOrDefaultAsync(d => d.Id == job.JobId, cancellationToken: stoppingToken);

        if (bgJob is null) {
          _logger.LogWarning("DeliveryExtractionJob with JobId {JobId} was not found; skipping it", job.JobId);
          return;
        }

        bgJob.Status = JobStatus.Processing;
        bgJob.AttemptCount++;
        bgJob.StartedAtUtc = DateTime.UtcNow;
        bgJob.CompletedAtUtc = null;
        await jenianDbContext.SaveChangesAsync(cancellationToken: stoppingToken);

        var openAi = scope.ServiceProvider.GetRequiredService<IOpenAiService>();
        var reportRepository = scope.ServiceProvider.GetRequiredService<ICWHReportRepository>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
        var parserService = scope.ServiceProvider.GetRequiredService<IParserService>();

        var ocrText = string.Empty;
        if (job.BlobNames is { Count: > 0 }) {
          ocrText = await BuildOcrTextAsync(job.BlobNames, blobStorage, parserService, stoppingToken);
        }

        // Format the assembled OCR text
        var answer = await openAi.DeliveryFormatter(ocrText, stoppingToken);
        _logger.LogInformation("DeliveryExtractorWorker processed job. Result: {Result}", answer);

        await reportRepository.UpdateAnswerToDeliveryAsync(job.JobId, answer, stoppingToken);
        var updated = await reportRepository.UpdateAnswerToEodReportAsync(job.UserId, job.ReportId, answer, stoppingToken);

        if (!updated) throw new DbUpdateException("EOD report was not found");

        bgJob.Status = JobStatus.Succeeded;
        bgJob.Result = answer;
        bgJob.CompletedAtUtc = DateTime.UtcNow;
        await jenianDbContext.SaveChangesAsync(cancellationToken: stoppingToken);

        _logger.LogInformation("DeliveryExtractionJob {JobId} completed successfully", job.JobId);

        await TrySendTelegramNotificationAsync(job, scope.ServiceProvider, stoppingToken);
      } catch (OperationCanceledException e) when (stoppingToken.IsCancellationRequested) {
        _logger.LogInformation(e, "DeliveryExtractorWorker operation was canceled for JobId {JobId}", job.JobId);
        await TrySetTerminalStatusAsync(job.JobId, JobStatus.Canceled);
      } catch (OperationCanceledException e) {
        _logger.LogError(e, "DeliveryExtractorWorker timed out for JobId {JobId}", job.JobId);
        await TrySetTerminalStatusAsync(job.JobId, JobStatus.Failed);
      } catch (Exception e) {
        _logger.LogError(e, "Failed processing DeliveryExtractorJob for JobId {JobId}", job.JobId);
        await TrySetTerminalStatusAsync(job.JobId, JobStatus.Failed);
      }
    }

    private async Task TrySetTerminalStatusAsync(Guid jobId, JobStatus status) {
      try {
        using var statusTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var scope = _scopeFactory.CreateAsyncScope();
        var jenianDbContext = scope.ServiceProvider.GetRequiredService<JenianDbContext>();

        var bgJob = await jenianDbContext.DeliveryExtractionJobs
          .FirstOrDefaultAsync(d => d.Id == jobId, cancellationToken: statusTimeout.Token);

        if (bgJob is null) {
          _logger.LogWarning("Cannot set status {Status}; DeliveryExtractionJob {JobId} was not found", status, jobId);
          return;
        }

        if (bgJob.Status == JobStatus.Succeeded) {
          _logger.LogWarning("DeliveryExtractionJob {JobId} already succeeded; status will not be changed to {Status}", jobId, status);
          return;
        }

        bgJob.Status = status;
        bgJob.CompletedAtUtc = DateTime.UtcNow;
        await jenianDbContext.SaveChangesAsync(statusTimeout.Token);
      } catch (Exception e) {
        _logger.LogError(e, "Failed to persist status {Status} for DeliveryExtractionJob {JobId}", status, jobId);
      }
    }

    private async Task TrySendTelegramNotificationAsync(
      DeliveryWorkerJob job,
      IServiceProvider serviceProvider,
      CancellationToken cancellationToken) {
      try {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var telegramMessenger = serviceProvider.GetRequiredService<ITelegramMessenger>();
        var reportRepository = serviceProvider.GetRequiredService<ICWHReportRepository>();

        var telegramUserId = await userManager.Users
          .Where(u => u.Id == job.UserId)
          .Select(u => u.TelegramUserId)
          .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (telegramUserId != null && long.TryParse(telegramUserId, out var telegramChatId)) {
          var report = await reportRepository.PopulateReportTemplateAsync(job.ReportId, job.UserId, cancellationToken);
          if (report != null) {
            _logger.LogInformation("Background worker report = {Report}", report);
            await telegramMessenger.SendMessageAsync(telegramChatId, report, cancellationToken);
          }
        }
      } catch (OperationCanceledException e) when (cancellationToken.IsCancellationRequested) {
        //TODO: Figure out where to let user know - Telegram bot or in-app notification
        _logger.LogInformation(e, "Telegram notification was canceled for completed JobId {JobId}", job.JobId);
      } catch (Exception e) {
        //TODO: Figure out where to let user know - Telegram bot or in-app notification
        _logger.LogError(e, "Telegram notification failed for completed JobId {JobId}", job.JobId);
      }
    }
  }
}
